using System.ComponentModel;

namespace Genius.PriceChecker.UI.Forms.ViewModels
{
    public interface ITabViewModel
    {
        IActionCommand Activated { get; }
        IActionCommand Deactivated { get; }
    }

    public abstract class TabViewModelBase<TViewModel> : ViewModelBase<TViewModel>, ITabViewModel
    {
        [Browsable(false)]
        public IActionCommand Activated { get; } = new ActionCommand();
        [Browsable(false)]
        public IActionCommand Deactivated { get; } = new ActionCommand();
    }
}
