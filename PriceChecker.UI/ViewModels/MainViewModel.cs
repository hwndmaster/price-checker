using System.Collections.Generic;
using Genius.PriceChecker.UI.Forms.ViewModels;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class MainViewModel : ViewModelBase<MainViewModel>
    {
        public MainViewModel(
            TrackerViewModel tracker,
            AgentsViewModel agents,
            SettingsViewModel settings,
            LogsViewModel logs)
        {
            Tabs = new List<ViewModelBase> {
                tracker,
                agents,
                settings,
                logs
            };
        }

        public List<ViewModelBase> Tabs { get; }

        public int SelectedTabIndex
        {
            get => GetOrDefault<int>();
            set => RaiseAndSetIfChanged(value, (@old, @new) => {
                Tabs[@old].Deactivated.Execute(null);
            });
        }
    }
}
