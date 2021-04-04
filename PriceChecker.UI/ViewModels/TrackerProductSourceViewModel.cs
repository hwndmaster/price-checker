using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;

namespace Genius.PriceChecker.UI.ViewModels
{
  public class TrackerProductSourceViewModel : ViewModelBase
    {
        public TrackerProductSourceViewModel(ProductSource productSource)
        {
            Agent = productSource?.AgentId;
            Argument = productSource?.AgentArgument;

            PropertiesAreInitialized = true;
        }

        [SelectFromList(nameof(TrackerProductViewModel.Agents), fromOwnerContext: true)]
        public string Agent
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        public string Argument
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [Icon("Trash16")]
        public IActionCommand DeleteCommand { get; } = new ActionCommand();
    }
}
