using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Infrastructure.Logging
{
    public sealed class LogEvent : IEventMessage
    {
        public LogEvent(LogLevel severity, string logger, string message)
        {
            Severity = severity;
            Logger = logger;
            Message = message;
        }

        public LogLevel Severity { get; }
        public string Logger { get; }
        public string Message { get; }
    }
}
