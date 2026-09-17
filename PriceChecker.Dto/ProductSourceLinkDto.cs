namespace Genius.PriceChecker.Dto;

/// <summary>
///   One of the pages a product's price is read from, as a link the products list can open.
/// </summary>
/// <param name="AgentKey">
///   The key of the agent the source is scanned with. It names the site to the user, so that a product
///   tracked on several sites can be told apart in the list.
/// </param>
/// <param name="Url">The page the agent's URL template resolves to for this source's argument.</param>
public sealed record ProductSourceLinkDto(string AgentKey, string Url);
