using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.WebApi.Services;

public interface IScanOrchestrator
{
    /// <summary>
    ///   Scans the specified products, or all products when <paramref name="productIds"/> is null.
    /// </summary>
    Task ScanAsync(IReadOnlyCollection<ProductRef>? productIds = null, CancellationToken cancellationToken = default);
}

internal sealed class ScanOrchestrator : IScanOrchestrator
{
    private const int MaxParallelProductScans = 4;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScanContext _scanContext;
    private readonly IScanNotifier _notifier;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(IServiceScopeFactory scopeFactory, IScanContext scanContext,
        IScanNotifier notifier, ILogger<ScanOrchestrator> logger)
    {
        _scopeFactory = scopeFactory.NotNull();
        _scanContext = scanContext.NotNull();
        _notifier = notifier.NotNull();
        _logger = logger.NotNull();
    }

    public async Task ScanAsync(IReadOnlyCollection<ProductRef>? productIds = null, CancellationToken cancellationToken = default)
    {
        ScanProduct[] scanProducts;
        using (var scope = _scopeFactory.CreateScope())
        {
            var productsRepo = scope.ServiceProvider.GetRequiredService<IProductsRepository>();
            scanProducts = (await productsRepo.GetScanProductsAsync(productIds, cancellationToken).ConfigureAwait(false))
                .Where(p => p.Sources.Length > 0)
                .ToArray();
        }

        if (scanProducts.Length == 0)
        {
            return;
        }

        _scanContext.NotifyScanStarted(scanProducts.Length);
        await _notifier.ScanProgressAsync(_scanContext.GetProgress()).ConfigureAwait(false);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelProductScans,
            CancellationToken = cancellationToken,
        };
        await Parallel.ForEachAsync(scanProducts, parallelOptions,
            async (product, token) => await ScanSingleProductAsync(product, token).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    private async Task ScanSingleProductAsync(ScanProduct product, CancellationToken cancellationToken)
    {
        var productRef = new ProductRef(product.ProductId);
        await _notifier.ProductScanStartedAsync(productRef).ConfigureAwait(false);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var priceSeeker = scope.ServiceProvider.GetRequiredService<IPriceSeeker>();
            var productsRepo = scope.ServiceProvider.GetRequiredService<IProductsRepository>();

            _logger.LogTrace("Scanning product '{ProductName}'", product.Name);

            var previousOverview = await productsRepo.GetOverviewByIdAsync(productRef, cancellationToken).ConfigureAwait(false);
            var results = await priceSeeker.SeekAsync(product, cancellationToken).ConfigureAwait(false);
            if (results.Length == 0)
            {
                _logger.LogWarning("Price scanning for '{ProductName}' failed or no results retrieved", product.Name);
                _scanContext.NotifyProductFinished(hasErrors: true, hasNewLowestPrice: false);
                await _notifier.ProductScanFailedAsync(productRef, "Scan failed or no results retrieved").ConfigureAwait(false);
                return;
            }

            await productsRepo.AddScanResultsAsync(productRef, results, cancellationToken).ConfigureAwait(false);

            var overview = await productsRepo.GetOverviewByIdAsync(productRef, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Product with ID '{productRef}' unexpectedly disappeared during the scan.");

            var hasNewLowestPrice = previousOverview?.LowestPrice is not null
                && overview.LowestPrice is not null
                && overview.LowestPrice < previousOverview.LowestPrice;
            var hasErrors = results.Any(r => r.Status != AgentHandlingStatus.Success);

            if (hasNewLowestPrice)
            {
                overview = overview with { Status = ProductScanStatus.ScannedNewLowest };
            }

            _scanContext.NotifyProductFinished(hasErrors, hasNewLowestPrice);
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
}
