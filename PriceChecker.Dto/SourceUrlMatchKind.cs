namespace Genius.PriceChecker.Dto;

/// <summary>
///   How an agent was matched against a pasted product URL.
///   Must be kept in sync with the <c>SourceUrlMatchKind</c> enum of the web client.
/// </summary>
public enum SourceUrlMatchKind
{
    /// <summary>
    ///   The agent's own URL pattern matched the URL.
    /// </summary>
    UrlPattern,

    /// <summary>
    ///   The agent has no URL pattern of its own and its URL template was matched in reverse.
    ///   A weaker match: the template says how an argument is turned into a URL, not which of the
    ///   URLs a site serves carry that argument.
    /// </summary>
    UrlTemplate
}
