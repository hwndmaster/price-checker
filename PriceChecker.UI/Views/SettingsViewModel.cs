using Genius.PriceChecker.Core.Repositories;
using Genius.Atom.UI.Forms;

namespace Genius.PriceChecker.UI.Views;

public interface ISettingsViewModel : ITabViewModel
{
}

internal sealed class SettingsViewModel : TabViewModelBase, ISettingsViewModel
{
    public SettingsViewModel(ISettingsRepository repo)
    {
        Guard.NotNull(repo);

        // Member initialization:
        var settings = repo.Get();

        AutoRefreshMinuteOptions = [
#if DEBUG
            new AutoRefreshOption("1 minute (DEBUG ONLY)", 1),
#endif
            new AutoRefreshOption("1 hour", 60),
            new AutoRefreshOption("3 hours", 180),
            new AutoRefreshOption("8 hours", 480),
            new AutoRefreshOption("1 day", 1440)
        ];

        AutoRefreshEnabled = settings.AutoRefreshEnabled;
        AutoRefreshMinutes = AutoRefreshMinuteOptions.FirstOrDefault(x => x.Value == settings.AutoRefreshMinutes)
            ?? AutoRefreshMinuteOptions[0];

        // Subscriptions:
        PropertyChanged += (sender, args) => {
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


    public record AutoRefreshOption(string Name, int Value);
}
