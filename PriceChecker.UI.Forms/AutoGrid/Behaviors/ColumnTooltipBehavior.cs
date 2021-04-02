using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Genius.PriceChecker.UI.Forms.Attributes;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnTooltipBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            var path = context.GetAttribute<TooltipSourceAttribute>()?.Path;
            if (path == null)
            {
                return;
            }

            var style = WpfHelpers.EnsureDefaultCellStyle(context.Args.Column);
            var binding = new Binding(path);
            style.Setters.Add(new Setter(ToolTipService.ToolTipProperty, binding));
        }
    }
}
