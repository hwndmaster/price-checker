using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;

namespace Genius.PriceChecker.Core.Services;

public interface IProductStatusProvider
{
    ProductScanStatus DetermineStatus(IReadOnlyCollection<PriceSnapshot> recentPrices);

    /// <summary>
    ///   Describes, per failing source, why the last scan of a product did not fully succeed.
    /// </summary>
    /// <param name="recentPrices">The most recent price of every product source.</param>
    /// <returns>
    ///   A newline-separated "&lt;agent&gt;: &lt;reason&gt;" list, or <c>null</c> when every source succeeded.
    /// </returns>
    string? DescribeIssues(IReadOnlyCollection<PriceSnapshot> recentPrices);
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

    public string? DescribeIssues(IReadOnlyCollection<PriceSnapshot> recentPrices)
    {
        Guard.NotNull(recentPrices);

        var issues = recentPrices
            .Where(x => x.Status != AgentHandlingStatus.Success)
            .Select(x => $"{x.AgentKey}: {DescribeStatus(x.Status)}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return issues.Length == 0 ? null : string.Join('\n', issues);
    }

    private static string DescribeStatus(AgentHandlingStatus status)
        => status switch
        {
            AgentHandlingStatus.CouldNotFetch => "the page could not be downloaded",
            AgentHandlingStatus.CouldNotMatch => "the price pattern did not match the page content",
            AgentHandlingStatus.CouldNotParse => "the matched value could not be parsed as a price",
            AgentHandlingStatus.InvalidPrice => "the parsed price is not a valid amount",
            _ => "the scan did not complete for an unknown reason",
        };
}
