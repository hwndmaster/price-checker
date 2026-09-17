using System.Globalization;

namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   Resolves the page a product source points at, by filling an agent's URL template with the
///   source's argument.
/// </summary>
public static class SourceUrl
{
    /// <summary>
    ///   Returns the URL the given agent template resolves to with the given argument.
    /// </summary>
    /// <remarks>
    ///   A template the argument cannot be applied to falls back to the template itself, so that a
    ///   misconfigured agent yields a URL that is merely wrong instead of failing everything that asks
    ///   for one - a products list will not load at all if building a single link throws.
    /// </remarks>
    public static string Resolve(string urlTemplate, string argument)
    {
        Guard.NotNull(urlTemplate);

        try
        {
            return string.Format(CultureInfo.InvariantCulture, urlTemplate, argument);
        }
        catch (FormatException)
        {
            return urlTemplate;
        }
    }
}
