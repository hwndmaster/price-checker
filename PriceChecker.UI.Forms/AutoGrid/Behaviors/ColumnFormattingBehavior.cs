using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnFormattingBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            var format = context.GetAttribute<DisplayFormatAttribute>();
            if (format == null)
            {
                return;
            }

            context.GetBinding().StringFormat = format.DataFormatString;
        }
    }
}
