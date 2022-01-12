using System.Text.RegularExpressions;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.AgentHandlers;

internal sealed class SimpleRegex : IAgentHandler
{
    private const char DEFAULT_DECIMAL_DELIMITER = '.';

    private readonly ILogger<SimpleRegex> _logger;

    public SimpleRegex(ILogger<SimpleRegex> logger)
    {
        _logger = logger;
    }

    public AgentHandlingStatus Handle(Agent agent, string content, out decimal? price)
    {
        var re = new Regex(agent.PricePattern);
        var match = re.Match(content);
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

        var priceConverted = decimal.TryParse(priceString, out var priceValue);
        if (!priceConverted)
        {
            _logger.LogError("Could not convert the price '{priceString}' to decimal.", priceString);
            price = null;
            return false;
        }

        price = priceValue;
        return true;
    }
}
