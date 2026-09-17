namespace Genius.PriceChecker.Dto;

/// <summary>
///   The outcome of handling a single product source by a scanning agent.
///   Must be kept in sync with the <c>AgentHandlingStatus</c> enum of the web client.
/// </summary>
public enum AgentHandlingStatus
{
    Unknown,
    Success,
    CouldNotFetch,
    CouldNotMatch,
    CouldNotParse,
    InvalidPrice
}
