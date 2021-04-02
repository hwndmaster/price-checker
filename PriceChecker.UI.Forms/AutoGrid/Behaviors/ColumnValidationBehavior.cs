using System.Windows;
using System.Windows.Controls;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnValidationBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            if (context.DataGrid.IsReadOnly || context.Args.Column.IsReadOnly)
            {
                return;
            }

            var columnBinding = context.GetBinding();
            if (columnBinding == null)
            {
                return;
            }

            //columnBinding.ValidatesOnDataErrors = true;
            columnBinding.ValidatesOnNotifyDataErrors = true;
            columnBinding.NotifyOnValidationError = true;
            ((DataGridBoundColumn)context.Args.Column).ElementStyle
                = (Style)Application.Current.FindResource("ValidatableCellElementStyle");
        }
    }
}
