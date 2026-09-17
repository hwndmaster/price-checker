using Genius.PriceChecker.Dto;

namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   What a scan changed about a product's prices.
/// </summary>
/// <param name="HasNewLowestPrice">
///   Whether the scan beat the lowest price ever recorded for the product. This drives the products
///   list, and is determined for every product regardless of whether it tracks a target price.
/// </param>
/// <param name="HasReachedTarget">
///   Whether the scan brought the product's currently available price down to its target price, from
///   above it or from no price at all.
/// </param>
/// <param name="TargetPrice">The target price the product tracks, or <c>null</c> when it tracks none.</param>
public readonly record struct PriceChange(bool HasNewLowestPrice, bool HasReachedTarget, decimal? TargetPrice)
{
    /// <summary>
    ///   Whether the change is worth telling the user about outside the app: reaching the target for a
    ///   product that has one, a new lowest price for a product that has not.
    /// </summary>
    public bool IsWorthReporting => TargetPrice is null ? HasNewLowestPrice : HasReachedTarget;
}

public interface IPriceChangeEvaluator
{
    /// <summary>
    ///   Compares a product's overview from before a scan with the one from after it.
    /// </summary>
    /// <param name="previous">The overview from before the scan, or <c>null</c> for a product scanned for the first time.</param>
    /// <param name="current">The overview from after the scan.</param>
    PriceChange Evaluate(ProductOverviewDto? previous, ProductOverviewDto current);
}

internal sealed class PriceChangeEvaluator : IPriceChangeEvaluator
{
    public PriceChange Evaluate(ProductOverviewDto? previous, ProductOverviewDto current)
    {
        Guard.NotNull(current);

        // A product with no price history has no lowest price to beat: its first scan establishes one
        // rather than undercutting one.
        var hasNewLowestPrice = previous?.LowestPrice is not null
            && current.LowestPrice is not null
            && current.LowestPrice < previous.LowestPrice;

        var target = current.TargetPrice;

        // Compared against the recent price rather than the lowest one: the lowest may date from
        // months ago and no longer be available, whereas the target is about what can be paid today.
        var hasReachedTarget = target is not null
            && current.RecentPrice is not null
            && current.RecentPrice <= target
            // Only the crossing counts. Without this, a product that simply stays below its target
            // would be reported again by every daily scan. Raising the target above a price that is
            // already below it is a change to the target, not to the price, and stays silent too.
            && (previous?.RecentPrice is null || previous.RecentPrice > target);

        return new PriceChange(hasNewLowestPrice, hasReachedTarget, target);
    }
}
