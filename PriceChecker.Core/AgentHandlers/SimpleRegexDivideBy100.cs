using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.AgentHandlers;

internal sealed class SimpleRegexDivideBy100 : IAgentHandler
{
    private readonly SimpleRegex _simpleRegex;

    public SimpleRegexDivideBy100(SimpleRegex simpleRegex)
    {
        _simpleRegex = simpleRegex;
    }

    public AgentHandlingStatus Handle(Agent agent, string content, out decimal? price)
    {
        var result = _simpleRegex.Handle(agent, content, out price);
        if (result == AgentHandlingStatus.Success)
        {
            price /= 100m;
        }

        return result;
    }
}
