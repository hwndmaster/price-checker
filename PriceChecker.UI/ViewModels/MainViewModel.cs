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
            Tabs = new() {
                tracker,
                agents,
                settings,
                logs
            };
        }

        public List<ITabViewModel> Tabs { get; }

        public int SelectedTabIndex
        {
            get => GetOrDefault<int>();
            set => RaiseAndSetIfChanged(value, (@old, @new) => {
                Tabs[@old].Deactivated.Execute(null);
                Tabs[@new].Activated.Execute(null);
            });
        }
    }
}
