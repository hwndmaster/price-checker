using System.ComponentModel;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnReadOnlyBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            var isReadOnly = context.GetAttribute<ReadOnlyAttribute>()?.IsReadOnly;

            if (isReadOnly == true)
            {
                context.Args.Column.IsReadOnly = true;
            }
        }
    }
}
