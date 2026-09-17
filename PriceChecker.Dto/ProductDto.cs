using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

public sealed record ProductDto(
    ProductRef Id,
    string Name,
    string? Category,
    string? Description,
    /// <summary>
    ///   The price at or below which the product is worth buying, or <c>null</c> when it tracks none.
    ///   A product that tracks one is reported when its price reaches it, rather than when it beats
    ///   its own lowest price.
    /// </summary>
    decimal? TargetPrice,
    ProductSourceDto[] Sources,
    DateTimeOffset DateCreated,
    DateTimeOffset LastModified) : IEntity<Guid, ProductRef>;
