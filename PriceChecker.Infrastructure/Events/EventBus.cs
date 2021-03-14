using System;
using System.Collections.Concurrent;

namespace Genius.PriceChecker.Infrastructure.Events
{
    public interface IEventBus
    {
        void Publish(IEventMessage @event);
        IDisposable Subscribe<T>(Action<T> action)
            where T : IEventMessage;
        IDisposable Subscribe<T1, T2>(Action action)
            where T1 : IEventMessage
            where T2 : IEventMessage;
        IDisposable Subscribe<T1, T2, T3>(Action action)
            where T1 : IEventMessage
            where T2 : IEventMessage
            where T3 : IEventMessage;
    }

    internal sealed class EventBus : IEventBus
    {
        private static ConcurrentDictionary<EventSubscription, object> _subscriptions = new ConcurrentDictionary<EventSubscription, object>();

        private event EventHandler<EventPublishedArgs> EventAdded;

        public EventBus()
        {
            EventAdded += (_, args) => {
                var events = new [] { args.Event };
                foreach (var @event in events)
                {
                    Handle(args.Event);
                }
            };
        }

        public void Publish(IEventMessage message)
        {
            EventAdded?.Invoke(this, new EventPublishedArgs(message));
        }

        public IDisposable Subscribe<T>(Action<T> action)
            where T : IEventMessage
        {
            var subscription = new EventSubscription<T>(this, action);
            _subscriptions.TryAdd(subscription, null);
            return subscription;
        }

        public IDisposable Subscribe<T1, T2>(Action action)
            where T1 : IEventMessage
            where T2 : IEventMessage
        {
            var subscription1 = Subscribe<T1>(x => action());
            var subscription2 = Subscribe<T2>(x => action());

            return new CompositeDisposable(subscription1, subscription2);
        }

        public IDisposable Subscribe<T1, T2, T3>(Action action)
            where T1 : IEventMessage
            where T2 : IEventMessage
            where T3 : IEventMessage
        {
            var subscription1 = Subscribe<T1>(x => action());
            var subscription2 = Subscribe<T2>(x => action());
            var subscription3 = Subscribe<T3>(x => action());

            return new CompositeDisposable(subscription1, subscription2, subscription3);
        }

        public void StopSubscription(EventSubscription subscription)
        {
            _subscriptions.TryRemove(subscription, out var _);
        }

        private void Handle(IEventMessage @event)
        {
            foreach (var subscription in _subscriptions)
            {
                if (subscription.Key.EventType == @event.GetType())
                {
                    subscription.Key.Action(@event);
                }
            }
        }
    }
}
