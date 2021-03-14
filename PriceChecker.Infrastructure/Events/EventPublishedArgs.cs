using System;

namespace Genius.PriceChecker.Infrastructure.Events
{
    internal sealed class EventPublishedArgs : EventArgs
    {
        public EventPublishedArgs(IEventMessage @event)
        {
            Event = @event;
        }

        public IEventMessage Event { get; }
    }
}