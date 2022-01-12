using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.AgentHandlers;

public interface IAgentHandler
{
    AgentHandlingStatus Handle(Agent agent, string content, out decimal? price);
}
