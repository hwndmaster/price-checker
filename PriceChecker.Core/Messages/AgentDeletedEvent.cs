using Genius.PriceChecker.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class AgentDeletedEvent : IEventMessage
    {
        public AgentDeletedEvent(Agent deletedAgent)
        {
            DeletedAgent = deletedAgent;
        }

        public Agent DeletedAgent { get; }
    }
}
