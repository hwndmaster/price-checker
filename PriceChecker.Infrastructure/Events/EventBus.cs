using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Genius.PriceChecker.Infrastructure.Events
{
    public interface IEventBus
    {
        void Publish(IEventMessage @event);
        IObservable<T> WhenFired<T>()
            where T : IEventMessage;
        IObservable<Unit> WhenFired<T1, T2>()
            where T1 : IEventMessage
            where T2 : IEventMessage;
        IObservable<Unit> WhenFired<T1, T2, T3>()
            where T1 : IEventMessage
            where T2 : IEventMessage
            where T3 : IEventMessage;
    }

    internal sealed class EventBus : IEventBus
    {
        private event EventHandler<EventPublishedArgs> _eventAdded;
        private readonly IObservable<EventPublishedArgs> _mainObservable;

        public EventBus()
        {
            _mainObservable = Observable.FromEventPattern<EventPublishedArgs>(
                x => this._eventAdded += x,
                x => this._eventAdded -= x)
                .Select(x => x.EventArgs);
        }

        public void Publish(IEventMessage message)
        {
            _eventAdded?.Invoke(this, new EventPublishedArgs(message));
        }

        public IObservable<T> WhenFired<T>()
            where T : IEventMessage
        {
            return _mainObservable
                .Where(x => x.Event is T)
                .Select(x => (T)x.Event);
        }

        public IObservable<Unit> WhenFired<T1, T2>()
            where T1 : IEventMessage
            where T2 : IEventMessage
        {
            return WhenFired<T1>().Select(x => Unit.Default)
                .Merge(WhenFired<T2>().Select(x => Unit.Default));
        }

        public IObservable<Unit> WhenFired<T1, T2, T3>()
            where T1 : IEventMessage
            where T2 : IEventMessage
            where T3 : IEventMessage
        {
            return WhenFired<T1, T2>()
                .Merge(WhenFired<T3>().Select(x => Unit.Default));
        }
    }
}
