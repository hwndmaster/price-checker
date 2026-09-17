using System.Text.RegularExpressions;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface ISourceUrlRecognizer
{
    /// <summary>
    ///   The named group an agent's URL pattern captures the agent argument with.
    /// </summary>
    const string ArgumentGroupName = "arg";

    /// <summary>
    ///   Finds the agents that can scan the given product URL and reads the agent argument off it.
    /// </summary>
    /// <param name="url">
    ///   A product URL as pasted by a user. A missing scheme is assumed to be <c>https</c>.
    /// </param>
    /// <param name="agents">The agents to match the URL against.</param>
    /// <returns>
    ///   The matching agents, the most specific one first, or an empty list when the URL is not a
    ///   web address or no agent recognizes it.
    /// </returns>
    IReadOnlyList<RecognizedSource> Recognize(string? url, IEnumerable<RecognizableAgent> agents);
}

/// <summary>
///   Recognizes which agent scans a pasted product URL.
/// </summary>
/// <remarks>
///   An agent describes how an argument becomes a URL (<see cref="RecognizableAgent.UrlTemplate"/>),
///   which is not enough to go the other way: a site normally serves a product under more URLs than
///   the single canonical one the template builds, and Amazon in particular serves the same ASIN
///   under any number of slug prefixes. Hence <see cref="RecognizableAgent.UrlPattern"/>, where an
///   agent states which URLs it accepts. The reverse-matched template is only the fallback for the
///   agents that have not got one.
/// </remarks>
internal sealed class SourceUrlRecognizer : ISourceUrlRecognizer
{
    private const string ArgumentPlaceholder = "{0}";
    private const string WwwPrefix = "www.";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly ILogger<SourceUrlRecognizer> _logger;

    public SourceUrlRecognizer(ILogger<SourceUrlRecognizer> logger)
    {
        _logger = logger.NotNull();
    }

    public IReadOnlyList<RecognizedSource> Recognize(string? url, IEnumerable<RecognizableAgent> agents)
    {
        Guard.NotNull(agents);

        if (!TryNormalizeUrl(url, out var normalizedUrl))
        {
            return [];
        }

        var matches = new List<(RecognizedSource Source, int ContextLength)>();

        foreach (var agent in agents)
        {
            var pattern = string.IsNullOrWhiteSpace(agent.UrlPattern)
                ? BuildPatternFromTemplate(agent)
                : agent.UrlPattern;
            if (pattern is null)
            {
                continue;
            }

            var kind = string.IsNullOrWhiteSpace(agent.UrlPattern)
                ? SourceUrlMatchKind.UrlTemplate
                : SourceUrlMatchKind.UrlPattern;

            if (TryMatch(agent, pattern, normalizedUrl, out var argument, out var contextLength))
            {
                matches.Add((new RecognizedSource(agent.AgentId, agent.Key, argument, kind), contextLength));
            }
        }

        // An explicit pattern beats a reverse-matched template, and among equals the agent that
        // recognized more of the URL around the argument is the more specific one. The key only
        // keeps the order stable.
        return matches
            .OrderBy(x => x.Source.MatchKind)
            .ThenByDescending(x => x.ContextLength)
            .ThenBy(x => x.Source.AgentKey, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Source)
            .ToList();
    }

    /// <param name="contextLength">
    ///   How much of the URL the pattern matched besides the argument itself, which is how specific
    ///   the agent is about the URLs it accepts.
    /// </param>
    private bool TryMatch(RecognizableAgent agent, string pattern, string url,
        out string argument, out int contextLength)
    {
        argument = string.Empty;
        contextLength = 0;

        Match match;
        try
        {
            match = Regex.Match(url, pattern, RegexOptions.IgnoreCase, MatchTimeout);
        }
        catch (ArgumentException ex)
        {
            // A URL pattern is authored by hand, so an invalid one only disqualifies its own agent.
            _logger.LogWarning(ex, "The URL pattern of the agent `{AgentKey}` is not a valid regular expression.", agent.Key);
            return false;
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "The URL pattern of the agent `{AgentKey}` timed out on `{Url}`.", agent.Key, url);
            return false;
        }

        if (!match.Success)
        {
            return false;
        }

        var group = match.Groups[ISourceUrlRecognizer.ArgumentGroupName];
        if (!group.Success || string.IsNullOrWhiteSpace(group.Value))
        {
            _logger.LogWarning("The URL pattern of the agent `{AgentKey}` matched `{Url}` but captured no `{GroupName}` group.",
                agent.Key, url, ISourceUrlRecognizer.ArgumentGroupName);
            return false;
        }

        argument = group.Value;
        contextLength = match.Length - group.Length;
        return true;
    }

    /// <summary>
    ///   Turns an agent's URL template into a pattern that matches the URLs of the same site: the
    ///   part of the template before the argument becomes a literal prefix, tolerant of the scheme,
    ///   of a <c>www.</c> the template does or does not carry and of a trailing slash, and everything
    ///   after the argument is dropped, since a pasted URL rarely carries the template's own query.
    /// </summary>
    private static string? BuildPatternFromTemplate(RecognizableAgent agent)
    {
        var placeholderIndex = agent.UrlTemplate?.IndexOf(ArgumentPlaceholder, StringComparison.Ordinal) ?? -1;
        if (placeholderIndex < 0)
        {
            // Without a placeholder the template resolves to one and the same URL for every
            // argument, so there is nothing to read off a pasted one.
            return null;
        }

        var prefix = agent.UrlTemplate![..placeholderIndex];
        if (!Uri.TryCreate(prefix, UriKind.Absolute, out var prefixUri) || string.IsNullOrEmpty(prefixUri.Host))
        {
            return null;
        }

        // A placeholder standing inside the host rather than the path leaves no literal site to
        // match a pasted URL against.
        var authorityEnd = agent.UrlTemplate.IndexOf('/', agent.UrlTemplate.IndexOf("://", StringComparison.Ordinal) + 3);
        if (authorityEnd < 0 || placeholderIndex < authorityEnd)
        {
            return null;
        }

        var host = prefixUri.Host.StartsWith(WwwPrefix, StringComparison.OrdinalIgnoreCase)
            ? prefixUri.Host[WwwPrefix.Length..]
            : prefixUri.Host;

        return $@"^https?://(?:www\.)?{Regex.Escape(host)}{Regex.Escape(prefixUri.AbsolutePath)}"
            + $@"(?<{ISourceUrlRecognizer.ArgumentGroupName}>[^?#]+?)/?(?:[?#]|$)";
    }

    private static bool TryNormalizeUrl(string? url, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        normalizedUrl = trimmed;
        return true;
    }
}
