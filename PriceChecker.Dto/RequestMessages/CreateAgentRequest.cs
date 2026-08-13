namespace Genius.PriceChecker.Dto.RequestMessages;

public sealed record CreateAgentRequest(
    string Key,
    string Url,
    string PricePattern,
    string Handler,
    string DecimalDelimiter);
