using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Services;

public interface IProductStatusProvider
{
    ProductScanStatus DetermineStatus(IReadOnlyCollection<PriceSnapshot> recentPrices);
}

internal sealed class ProductStatusProvider : IProductStatusProvider
{
    private readonly IDateTime _dateTime;
    private readonly IScanSchedule _scanSchedule;

    public ProductStatusProvider(IDateTime dateTime, IScanSchedule scanSchedule)
    {
        _dateTime = dateTime.NotNull();
        _scanSchedule = scanSchedule.NotNull();
    }

    public ProductScanStatus DetermineStatus(IReadOnlyCollection<PriceSnapshot> recentPrices)
    {
        Guard.NotNull(recentPrices);

        if (recentPrices.Count == 0)
            return ProductScanStatus.NotScanned;

        // Outdated means "the last scheduled scan should have refreshed this and did not". The grace
        // period keeps the whole list from reading as outdated while that scan is still in flight.
        var now = _dateTime.NowUtc;
        var trigger = _scanSchedule.GetMostRecentTrigger(now);
        if (recentPrices.Max(x => x.FoundDate) < trigger && now >= trigger + _scanSchedule.OutdatedGrace)
            return ProductScanStatus.Outdated;

        if (recentPrices.Any(x => x.Status != AgentHandlingStatus.Success))
            return ProductScanStatus.ScannedWithErrors;

        return ProductScanStatus.ScannedOk;
    }
}
