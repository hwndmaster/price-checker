using System.Globalization;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   Resolves the domain a product source is fetched from. The domain, not the agent, is the unit a
///   scanning session throttles by: several agents may point at the same site (one per price layout,
///   say, or one per locale of the same shop), and it is the site that rate-limits.
/// </summary>
public static class ScanDomain
{
    private const string WwwPrefix = "www.";

    /// <summary>
    ///   Returns the domain key of the given source.
    /// </summary>
    public static string Resolve(ScanSource source)
    {
        Guard.NotNull(source);

        return Resolve(source.Agent.Url, source.Argument);
    }

    /// <summary>
    ///   Returns the domain key of the URL an agent template resolves to with the given argument:
    ///   the host, lower-cased and without its <c>www.</c> prefix, so that a site reached under both
    ///   spellings is still scanned through a single queue.
    /// </summary>
    /// <remarks>
    ///   An agent whose URL does not resolve to an absolute one falls back to the template itself,
    ///   which keeps such an agent in a queue of its own instead of lumping every unparseable agent
    ///   together.
    /// </remarks>
    public static string Resolve(string urlTemplate, string argument)
    {
        Guard.NotNull(urlTemplate);

        // The argument is a part of the URL and may in principle carry the host itself, so the
        // template is resolved before the host is read off it.
        var url = SourceUrl.Resolve(urlTemplate, argument);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return urlTemplate.ToLowerInvariant();
        }

        var host = uri.Host.ToLowerInvariant();

        return host.StartsWith(WwwPrefix, StringComparison.Ordinal)
            ? host[WwwPrefix.Length..]
            : host;
    }
}
