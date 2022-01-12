using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Commands;

public sealed class AgentsStoreWithOverwriteCommand : ICommandMessage
{
    public AgentsStoreWithOverwriteCommand(IEnumerable<Agent> agents)
    {
        Agents = agents.ToArray();
    }

    public Agent[] Agents { get; }
}
