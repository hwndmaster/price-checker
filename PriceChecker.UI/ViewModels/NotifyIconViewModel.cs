using System.Windows;
using System.Windows.Input;
using Genius.PriceChecker.UI.Forms;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class NotifyIconViewModel
    {
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
