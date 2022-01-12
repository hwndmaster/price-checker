using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Messages;

public readonly record struct PriceSeekResult(
    AgentHandlingStatus Status,
    Guid ProductSourceId,
    string AgentKey,
    decimal? Price);
