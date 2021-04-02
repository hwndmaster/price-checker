using System.Windows.Input;
using Genius.PriceChecker.UI.Forms.Attributes;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnButtonBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            if (!typeof(ICommand).IsAssignableFrom(context.Property.PropertyType))
            {
                return;
            }

            var icon = context.GetAttribute<IconAttribute>()?.Name;

            context.Args.Column = WpfHelpers.CreateButtonColumn(context.Property.Name, icon);
        }
    }
}
