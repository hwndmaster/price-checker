using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto.RequestMessages;

public sealed record UpdateProductRequest(
    ProductRef Id,
    DateTimeOffset LastModified,
    string Name,
    string? Category,
    string? Description,
    UpdateProductSourceRequest[] Sources) : IPrimaryId<Guid, ProductRef>, ITimeStamped;

/// <summary>
///   A product source in an update request. Sources without <paramref name="Id"/> are
///   created, sources missing from the request are removed from the product.
/// </summary>
public sealed record UpdateProductSourceRequest(
    ProductSourceRef? Id,
    AgentRef AgentId,
    string AgentArgument);
