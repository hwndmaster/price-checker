using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.AgentHandlers;

public interface IAgentHandler
{
    AgentHandlingStatus Handle(ScanAgent agent, string content, out decimal? price);
}
