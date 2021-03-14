using System;

namespace Genius.PriceChecker.Infrastructure.Events
{
    internal abstract class EventSubscription : IDisposable
    {
        private readonly EventBus _eventBus;

        protected EventSubscription(EventBus eventBus, Type eventType)
        {
            _eventBus = eventBus;
            EventType = eventType;
        }

        public Action<IEventMessage> Action { get; protected set; }
        public Type EventType { get; protected set; }

        public void Dispose()
        {
            _eventBus.StopSubscription(this);
        }
    }

    internal sealed class EventSubscription<T> : EventSubscription
        where T : IEventMessage
    {
        public EventSubscription(EventBus eventBus, Action<T> action)
            : base(eventBus, typeof(T)) //, false)
        {
            Action = (ev) => action((T)ev);
        }
    }
}
