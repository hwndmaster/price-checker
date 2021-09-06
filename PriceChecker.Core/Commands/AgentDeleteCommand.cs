using System;
using Genius.Atom.Infrastructure.Commands;

namespace Genius.PriceChecker.Core.Commands
{
    public sealed class AgentDeleteCommand : ICommandMessage
    {
        public AgentDeleteCommand(Guid agentId)
        {
            AgentId = agentId;
        }

        public Guid AgentId { get; }
    }
}
