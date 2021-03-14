using System.Collections.Concurrent;
using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Infrastructure.Logging
{
    public sealed class EventBasedLoggerProvider : ILoggerProvider
    {
        private readonly IEventBus _eventBus;

        private readonly ConcurrentDictionary<string, EventBasedLogger> _loggers =
            new ConcurrentDictionary<string, EventBasedLogger>();

        public EventBasedLoggerProvider(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new EventBasedLogger(name, _eventBus));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
