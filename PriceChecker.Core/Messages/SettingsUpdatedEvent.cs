using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class SettingsUpdatedEvent : IEventMessage
    {
        public SettingsUpdatedEvent(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }
    }
}
