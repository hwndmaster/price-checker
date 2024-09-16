using System.Windows.Shell;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI.Views;

public interface IMainViewModel : IViewModel
{
}

internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    private readonly ITrackerScanContext _scanContext;
    private readonly INotifyIconViewModel _notifyViewModel;

    public MainViewModel(
        ITrackerViewModel tracker,
        IAgentsViewModel agents,
        ISettingsViewModel settings,
        ILogsTabViewModel logs,
        ITrackerScanContext scanContext,
        INotifyIconViewModel notifyViewModel)
    {
        // Dependencies:
        _scanContext = scanContext.NotNull();
        _notifyViewModel = notifyViewModel.NotNull();

        // Member initialization:
        Tabs = [
            tracker.NotNull(),
            agents.NotNull(),
            settings.NotNull(),
            logs.NotNull()
        ];

        // Subscriptions:
        _scanContext.ScanProgress.Subscribe(args => UpdateProgress(args.Status, args.Progress));
    }

    private void UpdateProgress(TrackerScanStatus status, double progress)
    {
        if (status == TrackerScanStatus.InProgress)
        {
            ProgressState = TaskbarItemProgressState.Normal;
            ProgressValue = progress;
        }
        else if (status == TrackerScanStatus.InProgressWithErrors)
        {
            ProgressState = TaskbarItemProgressState.Paused;
            ProgressValue = progress;
        }
        else if (status == TrackerScanStatus.Finished)
        {
            var message = _scanContext.HasNewLowestPrice ?
                "Prices for some products have become even lower! Check it out." :
                "Nothing interesting has been caught.";
            if (_scanContext.HasErrors)
            {
                message += Environment.NewLine + "NOTE: Some products could not finish scanning properly. Check the logs for details.";
            }
            _notifyViewModel.ShowBalloonTip("Scan finished", message,
                _scanContext.HasErrors ? BalloonIcon.Warning : BalloonIcon.Info);
            ProgressState = TaskbarItemProgressState.None;
            ProgressValue = 0;
        }
        else
        {
            ProgressState = TaskbarItemProgressState.None;
            ProgressValue = 0;
        }
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
