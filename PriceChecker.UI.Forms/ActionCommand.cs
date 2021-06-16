using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Genius.PriceChecker.UI.Forms
{
    public interface IActionCommand : ICommand
    {
        IObservable<Unit> Executed { get; }
    }

    public class ActionCommand : IActionCommand
    {
        private Func<object, Task> _asyncAction;
        private Predicate<object> _canExecute;
        private Subject<Unit> _executed = new();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public IObservable<Unit> Executed => _executed;

        public ActionCommand()
            : this (_ => { }, null)
        {
        }

        public ActionCommand(Func<object, Task> asyncAction)
            : this (asyncAction, null)
        {
        }

        public ActionCommand(Action<object> action)
            : this (action, null)
        {
        }

        public ActionCommand(Action<object> action, Predicate<object> canExecute)
            : this ((o) => { action(o); return Task.CompletedTask; }, canExecute)
        {
        }

        public ActionCommand(Func<object, Task> asyncAction, Predicate<object> canExecute)
        {
            if (asyncAction == null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            _asyncAction = asyncAction;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {
                return _canExecute.Invoke(parameter);
            }

            return true;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _asyncAction.Invoke(parameter);
                _executed.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Action failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
