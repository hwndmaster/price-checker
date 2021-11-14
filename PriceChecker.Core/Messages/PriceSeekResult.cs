namespace Genius.PriceChecker.Core.Messages;

public readonly record struct PriceSeekResult(
    Guid ProductSourceId,
    string AgentKey,
    decimal Price);
