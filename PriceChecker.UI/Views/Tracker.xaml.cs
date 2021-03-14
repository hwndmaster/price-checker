using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.ViewModels;

namespace Genius.PriceChecker.UI.Views
{
    [ExcludeFromCodeCoverage]
    public partial class Tracker
    {
        public Tracker()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => {
                WpfHelpers.AddFlyout<AddEditProductFlyout>(this, nameof(TrackerViewModel.IsAddEditProductVisible), nameof(TrackerViewModel.EditingProduct));
            };
        }
    }
}
