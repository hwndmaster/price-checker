using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Core.Models;

/// <summary>
///   An agent as the URL recognition sees it, detached from any persistence concerns.
/// </summary>
public sealed record RecognizableAgent(
    AgentRef AgentId,
    string Key,
    string UrlTemplate,
    string? UrlPattern);

/// <summary>
///   An agent that can scan a pasted product URL, together with the argument read off that URL.
/// </summary>
public sealed record RecognizedSource(
    AgentRef AgentId,
    string AgentKey,
    string AgentArgument,
    SourceUrlMatchKind MatchKind);
