using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Infrastructure.Logging;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.UI.ViewModels
{
  public class LogsViewModel : TabViewModelBase
    {
        public LogsViewModel(IEventBus eventBus)
        {
            eventBus.WhenFired<LogEvent>()
                .Subscribe(x => {
                    Application.Current.Dispatcher.Invoke(() => {
                        LogItems.Add(new LogItemViewModel { Severity = x.Severity, Logger = x.Logger, Message = x.Message });
                    });
                });

            CleanLogCommand = new ActionCommand(_ => {
                LogItems.Clear();
            });

            LogItems.CollectionChanged += (_, args) => {
                if (HasNewErrors)
                    return;
                HasNewErrors = args.NewItems?.Cast<LogItemViewModel>()
                    .Any(x => x.Severity >= LogLevel.Error) ?? false;
            };

            Activated.Executed.Subscribe(_ => HasNewErrors = false);
            Deactivated.Executed.Subscribe(_ => HasNewErrors = false);
        }

        public ObservableCollection<LogItemViewModel> LogItems { get; } = new ObservableCollection<LogItemViewModel>();

        public bool HasNewErrors
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        public IActionCommand CleanLogCommand { get; }
    }
}
