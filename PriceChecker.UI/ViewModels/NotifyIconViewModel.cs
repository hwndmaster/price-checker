using System;
using System.Windows;
using System.Windows.Input;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Helpers;
using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface INotifyIconViewModel
    {
        void ShowBalloonTip(string title, string message, BalloonIcon icon);
    }

    internal sealed class NotifyIconViewModel : INotifyIconViewModel
    {
        internal event EventHandler<ShowBalloonTipEventArgs> ShowBalloonTipTriggered;

        public void ShowBalloonTip(string title, string message, BalloonIcon icon)
        {
            ShowBalloonTipTriggered.Invoke(this, new ShowBalloonTipEventArgs {
                Title = title,
                Message = message,
                Icon = icon
            });
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
}
