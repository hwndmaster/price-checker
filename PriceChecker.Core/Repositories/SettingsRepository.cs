using System;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories
{
    public interface ISettingsRepository
    {
        Settings Get();
        void Store(Settings settings);
    }

    internal sealed class SettingsRepository : ISettingsRepository
    {
        private readonly IEventBus _eventBus;
        private readonly IPersister _persister;
        private readonly ILogger<SettingsRepository> _logger;

        private const string FILENAME = @".\settings.json";
        private Settings _settings = null;

        public SettingsRepository(IEventBus eventBus, IPersister persister, ILogger<SettingsRepository> logger)
        {
            _persister = persister;
            _logger = logger;
            _eventBus = eventBus;

            _settings = _persister.Load<Settings>(FILENAME) ?? CreateDefaultSettings();
        }

        public Settings Get() => _settings;

        public void Store(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("Settings cannot be null.");
            }

            _settings = settings;

            _persister.Store(FILENAME, settings);

            _eventBus.Publish(new SettingsUpdatedEvent(settings));

            _logger.LogInformation("Settings updated.");
        }

        private Settings CreateDefaultSettings()
            => new Settings {
                AutoRefreshEnabled = false,
                AutoRefreshMinutes = 1440 // 24 hours
            };
    }
}
