using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto.RequestMessages;

public sealed record CreateProductRequest(
    string Name,
    string? Category,
    string? Description,
    CreateProductSourceRequest[] Sources);

public sealed record CreateProductSourceRequest(
    AgentRef AgentId,
    string AgentArgument);
