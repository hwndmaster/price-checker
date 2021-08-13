using System.Linq;
using Genius.PriceChecker.Core.Repositories;
using Genius.Atom.UI.Forms.ViewModels;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface ISettingsViewModel : ITabViewModel
    {
    }

    internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel
    {
        public SettingsViewModel(ISettingsRepository repo)
        {
            var settings = repo.Get();

            AutoRefreshMinuteOptions = new [] {
#if DEBUG
                new AutoRefreshOption { Name = "1 minute (DEBUG ONLY)", Value = 1 },
#endif
                new AutoRefreshOption { Name = "1 hour", Value = 60 },
                new AutoRefreshOption { Name = "3 hours", Value = 180 },
                new AutoRefreshOption { Name = "8 hours", Value = 480 },
                new AutoRefreshOption { Name = "1 day", Value = 1440 }
            };

            AutoRefreshEnabled = settings.AutoRefreshEnabled;
            AutoRefreshMinutes = AutoRefreshMinuteOptions.FirstOrDefault(x => x.Value == settings.AutoRefreshMinutes)
                ?? AutoRefreshMinuteOptions[0];

            this.PropertyChanged += (sender, args) => {
                settings.AutoRefreshEnabled = AutoRefreshEnabled;
                settings.AutoRefreshMinutes = AutoRefreshMinutes.Value;
                repo.Store(settings);
            };
        }

        public bool AutoRefreshEnabled
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        public AutoRefreshOption[] AutoRefreshMinuteOptions
        {
            get => GetOrDefault<AutoRefreshOption[]>();
            set => RaiseAndSetIfChanged(value);
        }

        public AutoRefreshOption AutoRefreshMinutes
        {
            get => GetOrDefault<AutoRefreshOption>();
            set => RaiseAndSetIfChanged(value);
        }


        public class AutoRefreshOption
        {
            public string Name { get; init; }
            public int Value { get; init; }
        }
    }
}
