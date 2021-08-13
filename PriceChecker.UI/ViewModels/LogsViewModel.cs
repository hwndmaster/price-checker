using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Logging;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.ViewModels;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface ILogsViewModel : ITabViewModel
    {
    }

    internal sealed class LogsViewModel : TabViewModelBase, ILogsViewModel
    {
        public LogsViewModel(IEventBus eventBus)
        {
            eventBus.WhenFired<LogEvent>()
                .Subscribe(x => {
                    Application.Current.Dispatcher.Invoke(() =>
                        LogItems.Add(new LogItemViewModel { Severity = x.Severity, Logger = x.Logger, Message = x.Message })
                    );
                });

            CleanLogCommand = new ActionCommand(_ => LogItems.Clear());

            LogItems.CollectionChanged += (_, args) => {
                if (HasNewErrors)
                    return;
                HasNewErrors = args.NewItems?.Cast<ILogItemViewModel>()
                    .Any(x => x.Severity >= LogLevel.Error) ?? false;
            };

            Activated.Executed.Subscribe(_ => HasNewErrors = false);
            Deactivated.Executed.Subscribe(_ => HasNewErrors = false);
        }

        public ObservableCollection<ILogItemViewModel> LogItems { get; }
            = new TypedObservableList<ILogItemViewModel, LogItemViewModel>();

        public bool HasNewErrors
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        public IActionCommand CleanLogCommand { get; }
    }
}
