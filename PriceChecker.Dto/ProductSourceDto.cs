using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

public sealed record ProductSourceDto(
    ProductSourceRef Id,
    AgentRef AgentId,
    string AgentArgument);
