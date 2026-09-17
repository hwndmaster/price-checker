using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.WebApi.Services;

/// <summary>
///   What started a scanning session. Only an automatic scan reports its price alerts outwards: a
///   scan the user started is one they are watching in the app, where the outcome is already in
///   front of them, notification included.
/// </summary>
public enum ScanTrigger
{
    /// <summary>
    ///   A scan the user asked for — the Scan buttons of the products list, and the scan that follows
    ///   adding a product.
    /// </summary>
    Manual,

    /// <summary>
    ///   The daily scan run by <see cref="ScheduledScanHostedService"/>.
    /// </summary>
    Scheduled,
}

public interface IScanOrchestrator
{
    /// <summary>
    ///   Scans the specified products, or all products when <paramref name="productIds"/> is null.
    /// </summary>
    Task ScanAsync(IReadOnlyCollection<ProductRef>? productIds = null,
        ScanTrigger trigger = ScanTrigger.Manual, CancellationToken cancellationToken = default);
}

/// <summary>
///   Drives a scanning session: it collects what is to be scanned, hands it to the
///   <see cref="IScanSessionRunner"/>, which decides in which order and at which pace the sources are
///   fetched, and lets a <see cref="ScanSession"/> turn what comes back into stored prices, pushed
///   notifications and, for an automatic scan, one reported set of price alerts.
/// </summary>
internal sealed class ScanOrchestrator : IScanOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScanSessionRunner _sessionRunner;
    private readonly IScanContext _scanContext;
    private readonly IScanNotifier _notifier;
    private readonly IPriceChangeEvaluator _priceChangeEvaluator;

    // Everything a scan has to say is said by the session it creates, so the logger this class holds is
    // the session's own. Built from the factory rather than injected as ILogger<ScanSession>, so that
    // the sessions still log under their own name without this class asking for another type's logger.
    private readonly ILogger<ScanSession> _sessionLogger;

    public ScanOrchestrator(IServiceScopeFactory scopeFactory, IScanSessionRunner sessionRunner,
        IScanContext scanContext, IScanNotifier notifier, IPriceChangeEvaluator priceChangeEvaluator,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory.NotNull();
        _sessionRunner = sessionRunner.NotNull();
        _scanContext = scanContext.NotNull();
        _notifier = notifier.NotNull();
        _priceChangeEvaluator = priceChangeEvaluator.NotNull();
        _sessionLogger = loggerFactory.NotNull().CreateLogger<ScanSession>();
    }

    public async Task ScanAsync(IReadOnlyCollection<ProductRef>? productIds = null,
        ScanTrigger trigger = ScanTrigger.Manual, CancellationToken cancellationToken = default)
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

        var session = new ScanSession(trigger, _scopeFactory, _scanContext, _notifier, _priceChangeEvaluator, _sessionLogger);
        await _sessionRunner.RunAsync(scanProducts, session, cancellationToken).ConfigureAwait(false);
        await session.ReportAlertsAsync(cancellationToken).ConfigureAwait(false);
    }
}
