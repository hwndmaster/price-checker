using System;
using System.Collections.Generic;
using System.Windows.Shell;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            TrackerViewModel tracker,
            AgentsViewModel agents,
            SettingsViewModel settings,
            LogsViewModel logs,
            ITrackerScanContext scanContext,
            INotifyIconViewModel notifyViewModel)
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
                else if (args.Status == Helpers.TrackerScanStatus.Finished)
                {
                    var message = scanContext.HasNewLowestPrice
                            ? "Prices for some products have become even lower! Check it out."
                            : "Nothing interesting has been caught.";
                    if (scanContext.HasErrors)
                    {
                        message += Environment.NewLine + "NOTE: Some products could not finish scanning properly. Check the logs for details.";
                    }
                    notifyViewModel.ShowBalloonTip("Scan finished", message,
                        scanContext.HasErrors ? BalloonIcon.Warning : BalloonIcon.Info);
                    ProgressState = TaskbarItemProgressState.None;
                    ProgressValue = 0;
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
