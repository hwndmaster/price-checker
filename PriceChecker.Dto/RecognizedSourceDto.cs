using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

/// <summary>
///   An agent that recognized a pasted product URL, with the agent argument read off it.
/// </summary>
public sealed record RecognizedSourceDto(
    AgentRef AgentId,
    string AgentKey,
    string AgentArgument,
    SourceUrlMatchKind MatchKind);
