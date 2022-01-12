namespace Genius.PriceChecker.Core.Models;

public enum AgentHandlingStatus
{
    Unknown,
    Success,
    CouldNotFetch,
    CouldNotMatch,
    CouldNotParse,
    InvalidPrice
}
