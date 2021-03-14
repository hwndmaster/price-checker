using System.Windows;
using System.Collections.ObjectModel;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Infrastructure.Logging;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.ViewModels;
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
        public LogLevel Severity { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
    }
}
