using System.Windows;
using System.Windows.Input;
using Genius.Atom.UI.Forms;
using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI.ViewModels;

public interface INotifyIconViewModel
{
    void ShowBalloonTip(string title, string message, BalloonIcon icon);
}

internal sealed class NotifyIconViewModel : INotifyIconViewModel
{
    public readonly record struct ShowBalloonTipEventArgs(string Title, string Message, BalloonIcon Icon);

    internal event EventHandler<ShowBalloonTipEventArgs> ShowBalloonTipTriggered = (_, __) => {};

    public void ShowBalloonTip(string title, string message, BalloonIcon icon)
    {
        ShowBalloonTipTriggered.Invoke(this, new ShowBalloonTipEventArgs(title, message, icon));
    }

    public ICommand ShowWindowCommand => new ActionCommand(_ =>
        {
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.Focus();
        });

    public ICommand HideWindowCommand => new ActionCommand(_ =>
        Application.Current.MainWindow.Hide());

    public ICommand ExitApplicationCommand => new ActionCommand(_ =>
        Application.Current.Shutdown());
}
