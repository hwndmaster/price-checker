using System.Globalization;
using System.Text.RegularExpressions;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.AgentHandlers;

internal sealed class SimpleRegex : IAgentHandler
{
    private const char DEFAULT_DECIMAL_DELIMITER = '.';

    /// <summary>
    ///   A price pattern is written by hand and applied to a whole downloaded page, so a pattern
    ///   that backtracks badly would otherwise hold up the scanning session it runs in.
    /// </summary>
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<SimpleRegex> _logger;

    public SimpleRegex(ILogger<SimpleRegex> logger)
    {
        _logger = logger;
    }

    public AgentHandlingStatus Handle(ScanAgent agent, string content, out decimal? price)
    {
        Match match;
        try
        {
            match = Regex.Match(content, agent.PricePattern, RegexOptions.None, MatchTimeout);
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogError(ex, "The price pattern of the agent `{AgentKey}` timed out.", agent.Key);
            price = null;
            return AgentHandlingStatus.CouldNotMatch;
        }

        if (!match.Success)
        {
            price = null;
            return AgentHandlingStatus.CouldNotMatch;
        }

        if (!TryParsePrice(match, agent.DecimalDelimiter, out price))
        {
            return AgentHandlingStatus.CouldNotParse;
        }

        if (price <= 0.0m)
        {
            price = null;
            return AgentHandlingStatus.InvalidPrice;
        }

        return AgentHandlingStatus.Success;
    }

    private bool TryParsePrice(Match match, char decimalDelimiter, out decimal? price)
    {
        var priceString = match.Groups["price"].Value;
        if (decimalDelimiter != DEFAULT_DECIMAL_DELIMITER)
            priceString = priceString.Replace(decimalDelimiter, DEFAULT_DECIMAL_DELIMITER);

        // The delimiter normalization above only means anything when the parse that follows reads a
        // '.' as the decimal point: under a culture that groups by '.' the very same string would
        // turn "158.16" into 15816.
        var priceConverted = decimal.TryParse(priceString, NumberStyles.Number, CultureInfo.InvariantCulture, out var priceValue);
        if (!priceConverted)
        {
            _logger.LogError("Could not convert the price '{PriceString}' to decimal.", priceString);
            price = null;
            return false;
        }

        price = priceValue;
        return true;
    }
}
