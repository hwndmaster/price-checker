using Genius.PriceChecker.Core.Models;
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
    ProductScanStatus Status,
    decimal? LowestPrice,
    DateTimeOffset? LowestFoundDate,
    decimal? RecentPrice,
    DateTimeOffset? LastScannedDate,
    DateTimeOffset LastModified);
