using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Genius.PriceChecker.UI.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace Genius.PriceChecker.UI;

[ExcludeFromCodeCoverage]
public partial class NotifyIconResources : ResourceDictionary
{
    public NotifyIconResources()
    {
        InitializeComponent();

        var notifyIcon = (TaskbarIcon)this["NotifyIcon"];
        var viewModel = (NotifyIconViewModel)notifyIcon.DataContext;

        viewModel.ShowBalloonTipTriggered += (_, args) =>
            notifyIcon.ShowBalloonTip(args.Title, args.Message, args.Icon);
    }
}
