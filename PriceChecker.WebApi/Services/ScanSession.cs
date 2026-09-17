using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.WebApi.Services;

/// <summary>
///   One scanning session. It turns what each product's scan came back with into stored prices and
///   pushed notifications, and keeps the price alerts of the whole session together so that they are
///   reported as one notification at the end rather than one per product.
/// </summary>
/// <remarks>
///   One instance per scan, created by <see cref="ScanOrchestrator"/> rather than resolved: the
///   alerts it collects belong to that scan alone, so a manual scan started while the daily run is
///   under way cannot report the daily run's findings.
/// </remarks>
internal sealed class ScanSession : IScanSessionObserver
{
    private readonly Lock _lock = new();
    private readonly List<PriceAlert> _alerts = [];
    private readonly ScanTrigger _trigger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScanContext _scanContext;
    private readonly IScanNotifier _notifier;
    private readonly IPriceChangeEvaluator _priceChangeEvaluator;
    private readonly ILogger<ScanSession> _logger;

    public ScanSession(ScanTrigger trigger, IServiceScopeFactory scopeFactory, IScanContext scanContext,
        IScanNotifier notifier, IPriceChangeEvaluator priceChangeEvaluator, ILogger<ScanSession> logger)
    {
        _trigger = trigger;
        _scopeFactory = scopeFactory.NotNull();
        _scanContext = scanContext.NotNull();
        _notifier = notifier.NotNull();
        _priceChangeEvaluator = priceChangeEvaluator.NotNull();
        _logger = logger.NotNull();
    }

    public Task OnProductStartedAsync(ScanProduct product, CancellationToken cancellationToken)
    {
        Guard.NotNull(product);

        return _notifier.ProductScanStartedAsync(new ProductRef(product.ProductId));
    }

    public async Task OnProductScannedAsync(ScanProduct product, IReadOnlyCollection<PriceSeekResult> results,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(product);
        Guard.NotNull(results);

        var productRef = new ProductRef(product.ProductId);

        try
        {
            if (results.Count == 0)
            {
                _logger.LogWarning("Price scanning for '{ProductName}' failed or no results retrieved", product.Name);
                _scanContext.NotifyProductFinished(hasErrors: true, hasNewLowestPrice: false);
                await _notifier.ProductScanFailedAsync(productRef, "Scan failed or no results retrieved").ConfigureAwait(false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var productsRepo = scope.ServiceProvider.GetRequiredService<IProductsRepository>();

            var previousOverview = await productsRepo.GetOverviewByIdAsync(productRef, cancellationToken).ConfigureAwait(false);

            await productsRepo.AddScanResultsAsync(productRef, results, cancellationToken).ConfigureAwait(false);

            var overview = await productsRepo.GetOverviewByIdAsync(productRef, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Product with ID '{productRef}' unexpectedly disappeared during the scan.");

            var change = _priceChangeEvaluator.Evaluate(previousOverview, overview);
            var hasErrors = results.Any(r => r.Status != AgentHandlingStatus.Success);

            // A new lowest price is one whether or not the product tracks a target, and the list says
            // so either way; the target only decides what is worth reporting outwards.
            if (change.HasNewLowestPrice)
            {
                overview = overview with { Status = ProductScanStatus.ScannedNewLowest };
            }

            if (PriceAlert.From(previousOverview, overview, change) is { } alert)
            {
                lock (_lock)
                {
                    _alerts.Add(alert);
                }
            }

            _scanContext.NotifyProductFinished(hasErrors, change.HasNewLowestPrice);
            await _notifier.ProductScanFinishedAsync(overview).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price scanning for '{ProductName}' failed", product.Name);
            _scanContext.NotifyProductFinished(hasErrors: true, hasNewLowestPrice: false);
            await _notifier.ProductScanFailedAsync(productRef, ex.Message).ConfigureAwait(false);
        }
        finally
        {
            await _notifier.ScanProgressAsync(_scanContext.GetProgress()).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///   Reports what the session found, once, after every product has been scanned.
    /// </summary>
    public async Task ReportAlertsAsync(CancellationToken cancellationToken)
    {
        if (_trigger != ScanTrigger.Scheduled)
        {
            return;
        }

        PriceAlert[] alerts;
        lock (_lock)
        {
            alerts = [.. _alerts];
        }

        if (alerts.Length == 0)
        {
            return;
        }

        try
        {
            // Resolved per session rather than held for the lifetime of the orchestrator: the notifier
            // depends on a typed HttpClient, which the factory expects to be short-lived.
            using var scope = _scopeFactory.CreateScope();
            var alertNotifier = scope.ServiceProvider.GetRequiredService<IPriceAlertNotifier>();
            await alertNotifier.NotifyAsync(alerts, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The scan itself has already succeeded and its results are stored; a notification that
            // could not be delivered must not turn that into a failure.
            _logger.LogError(ex, "Reporting the price alerts of the scan failed.");
        }
    }
}
