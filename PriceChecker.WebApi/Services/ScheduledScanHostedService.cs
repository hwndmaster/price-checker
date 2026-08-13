using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.WebApi.Services;

/// <summary>
///   Re-scans the product prices once a day, at the moment configured in <c>Scanning:Schedule</c>.
///   Whether a run is due is derived from the products' own last scan dates rather than from an
///   in-memory timer, so a restart or an overnight shutdown does not skip a day: the run simply
///   happens as soon as the application is up again.
/// </summary>
internal sealed class ScheduledScanHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IScanSchedule _scanSchedule;
    private readonly IDateTime _dateTime;
    private readonly ILogger<ScheduledScanHostedService> _logger;

    public ScheduledScanHostedService(IServiceScopeFactory scopeFactory, IScanOrchestrator scanOrchestrator,
        IScanSchedule scanSchedule, IDateTime dateTime, ILogger<ScheduledScanHostedService> logger)
    {
        _scopeFactory = scopeFactory.NotNull();
        _scanOrchestrator = scanOrchestrator.NotNull();
        _scanSchedule = scanSchedule.NotNull();
        _dateTime = dateTime.NotNull();
        _logger = logger.NotNull();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_scanSchedule.Enabled)
        {
            _logger.LogInformation("The scheduled price scan is disabled.");
            return;
        }

        // Let the application settle before the first check, which may start a scan straight away
        // when the previous one was missed.
        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await RunIfDueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The scheduled price scan has failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    /// <summary>
    ///   Returns the products to scan: none unless the most recent scheduled scan has been missed,
    ///   otherwise every product not covered by it.
    /// </summary>
    internal static ProductRef[] SelectDueProducts(IReadOnlyCollection<ProductOverviewDto> overviews,
        DateTimeOffset trigger)
    {
        Guard.NotNull(overviews);

        if (overviews.Count == 0)
        {
            return [];
        }

        // Anything scanned at or after the trigger means the scheduled run already happened.
        var newestScan = overviews.Max(x => x.LastScannedDate);
        if (newestScan >= trigger)
        {
            return [];
        }

        return overviews
            .Where(x => x.LastScannedDate is null || x.LastScannedDate < trigger)
            .Select(x => x.Id)
            .ToArray();
    }

    private async Task RunIfDueAsync(CancellationToken cancellationToken)
    {
        var trigger = _scanSchedule.GetMostRecentTrigger(_dateTime.NowUtc);

        ProductRef[] productIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var overviews = await scope.ServiceProvider.GetRequiredService<IProductsRepository>()
                .GetOverviewAsync(cancellationToken).ConfigureAwait(false);
            productIds = SelectDueProducts(overviews.ToArray(), trigger);
        }

        if (productIds.Length == 0)
        {
            return;
        }

        _logger.LogInformation("The scheduled price scan of {ProductCount} product(s) started, for the run due at {Trigger:u}.",
            productIds.Length, trigger);

        await _scanOrchestrator.ScanAsync(productIds, cancellationToken).ConfigureAwait(false);
    }
}
