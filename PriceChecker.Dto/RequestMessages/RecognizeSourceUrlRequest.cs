namespace Genius.PriceChecker.Dto.RequestMessages;

/// <summary>
///   Asks which agents can scan the product page at <paramref name="Url"/>.
/// </summary>
public sealed record RecognizeSourceUrlRequest(string Url);
