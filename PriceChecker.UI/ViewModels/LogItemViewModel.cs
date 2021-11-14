using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Genius.Atom.UI.Forms;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.UI.ViewModels;

public interface ILogItemViewModel : IViewModel
{
    LogLevel Severity { get; }
}

internal sealed class LogItemViewModel : ViewModelBase, ILogItemViewModel
{
    public LogItemViewModel()
    {
        CopyToClipboardCommand = new ActionCommand(_ =>
            Clipboard.SetText(Message));
    }

    [IconSource(nameof(SeverityIcon), 16d)]
    public LogLevel Severity { get; set; }
    public string Logger { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [Browsable(false)]
    public BitmapImage? SeverityIcon
    {
        get
        {
            var icon = Severity switch
            {
                LogLevel.Warning => "Warning16",
                LogLevel.Error => "Error16",
                LogLevel.Critical => "Alert32",
                {} => null
            };
            if (icon is null)
                return null;
            return (BitmapImage)App.Current.FindResource(icon);
        }
    }

    [Browsable(false)]
    public bool IsSeverityCritical => Severity == LogLevel.Critical;

    [Icon("Copy16")]
    public IActionCommand CopyToClipboardCommand { get; }
}
