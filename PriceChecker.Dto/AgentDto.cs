using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

public sealed record AgentDto(
    AgentRef Id,
    string Key,
    string Url,
    string PricePattern,
    string Handler,
    string DecimalDelimiter,
    DateTimeOffset DateCreated,
    DateTimeOffset LastModified) : IEntity<Guid, AgentRef>;
