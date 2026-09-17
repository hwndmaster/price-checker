using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

/// <summary>
///   A read model for the products list: the product enriched with the outcome
///   of the latest price scans.
/// </summary>
public sealed record ProductOverviewDto(
    ProductRef Id,
    string Name,
    string? Category,
    string? Description,
    /// <summary>
    ///   The pages this product's price is read from, ordered by agent key. Carried by the list itself
    ///   rather than fetched when a name is clicked: the click opens a window, and a browser only allows
    ///   that while it is still handling the gesture, which an intervening request would outlast.
    /// </summary>
    IReadOnlyList<ProductSourceLinkDto> Sources,
    ProductScanStatus Status,
    string? StatusText,
    decimal? LowestPrice,
    DateTimeOffset? LowestFoundDate,
    decimal? RecentPrice,
    /// <summary>
    ///   The price at or below which the product is worth buying, or <c>null</c> when it tracks none.
    /// </summary>
    decimal? TargetPrice,
    DateTimeOffset? LastScannedDate,
    DateTimeOffset LastModified);
