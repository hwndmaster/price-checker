using System.Windows.Input;
using Genius.PriceChecker.UI.Forms.Attributes;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnDisplayIndexBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            var index = context.GetAttribute<DisplayIndexAttribute>()?.Index;

            if (index.HasValue)
            {
                context.Args.Column.DisplayIndex = index.Value;
            }
        }
    }
}
