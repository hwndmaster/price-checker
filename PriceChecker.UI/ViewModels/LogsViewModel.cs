using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Infrastructure.Logging;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Forms.Attributes;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class LogsViewModel : ViewModelBase<LogsViewModel>
    {
        public LogsViewModel(IEventBus eventBus)
        {
            eventBus.Subscribe<LogEvent>(x => {
                Application.Current.Dispatcher.Invoke(() => {
                    LogItems.Add(new LogItemViewModel { Severity = x.Severity, Logger = x.Logger, Message = x.Message });
                });
            });

            CleanLogCommand = new ActionCommand(_ => {
                LogItems.Clear();
            });
        }

        public IActionCommand CleanLogCommand { get; }
        public ObservableCollection<LogItemViewModel> LogItems { get; } = new ObservableCollection<LogItemViewModel>();
    }

    public class LogItemViewModel : ViewModelBase<LogItemViewModel>
    {
        [IconSource(nameof(SeverityIcon), 16d)]
        public LogLevel Severity { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }

        [Browsable(false)]
        public BitmapImage SeverityIcon
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
                if (icon == null)
                    return null;
                return (BitmapImage)App.Current.FindResource(icon);
            }
        }

        [Browsable(false)]
        public bool IsSeverityCritical => Severity == LogLevel.Critical;
    }
}
