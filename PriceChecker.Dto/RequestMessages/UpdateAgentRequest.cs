using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto.RequestMessages;

public sealed record UpdateAgentRequest(
    AgentRef Id,
    DateTimeOffset LastModified,
    string Key,
    string Url,
    string PricePattern,
    string Handler,
    string DecimalDelimiter) : IPrimaryId<Guid, AgentRef>, ITimeStamped;
