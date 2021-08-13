using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.UI.ViewModels;

namespace Genius.PriceChecker.UI.Views
{
    [ExcludeFromCodeCoverage]
    public partial class Tracker
    {
        public Tracker()
        {
            InitializeComponent();

            this.Loaded += (sender, args) =>
                WpfHelpers.AddFlyout<AddEditProductFlyout>(this, nameof(TrackerViewModel.IsAddEditProductVisible), nameof(TrackerViewModel.EditingProduct));
        }

        private void Filter_KeyUp(object sender, KeyEventArgs e)
        {
            var filterTextbox = (TextBox)sender;

            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                if (e.Key == Key.Escape)
                {
                    filterTextbox.Text = string.Empty;
                }

                BindingExpression binding = BindingOperations.GetBindingExpression(filterTextbox, TextBox.TextProperty);
                if (binding != null)
                    binding.UpdateSource();
            }
        }
    }
}
