using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

public sealed record ProductDto(
    ProductRef Id,
    string Name,
    string? Category,
    string? Description,
    ProductSourceDto[] Sources,
    DateTimeOffset DateCreated,
    DateTimeOffset LastModified) : IEntity<Guid, ProductRef>;
