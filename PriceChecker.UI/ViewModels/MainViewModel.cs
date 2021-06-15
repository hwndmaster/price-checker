using System;
using System.Collections.Generic;
using System.Windows.Shell;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            TrackerViewModel tracker,
            AgentsViewModel agents,
            SettingsViewModel settings,
            LogsViewModel logs,
            ITrackerScanContext scanContext)
        {
            Tabs = new() {
                tracker,
                agents,
                settings,
                logs
            };

            scanContext.ScanProgress.Subscribe(args => {
                if (args.Status == Helpers.TrackerScanStatus.InProgress)
                {
                    ProgressState = TaskbarItemProgressState.Normal;
                    ProgressValue = args.Progress;
                }
                else if (args.Status == Helpers.TrackerScanStatus.InProgressWithErrors)
                {
                    ProgressState = TaskbarItemProgressState.Paused;
                    ProgressValue = args.Progress;
                }
                else
                {
                    ProgressState = TaskbarItemProgressState.None;
                    ProgressValue = 0;
                }
            });
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

        public TaskbarItemProgressState ProgressState
        {
            get => GetOrDefault(TaskbarItemProgressState.None);
            set => RaiseAndSetIfChanged(value);
        }

        public double ProgressValue
        {
            get => GetOrDefault(0d);
            set => RaiseAndSetIfChanged(value);
        }
    }
}
