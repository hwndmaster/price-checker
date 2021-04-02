using System.Collections;
using Genius.PriceChecker.UI.Forms.Attributes;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnComboboxBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            if (context.DataGrid.IsReadOnly || context.Args.Column.IsReadOnly)
            {
                return;
            }

            var selectFromListAttr = context.GetAttribute<SelectFromListAttribute>();
            if (selectFromListAttr == null)
            {
                return;
            }

            if (selectFromListAttr.FromOwnerContext)
            {
                var prop = context.DataGrid.DataContext.GetType().GetProperty(selectFromListAttr.CollectionPropertyName);
                var value = prop.GetValue(context.DataGrid.DataContext) as IEnumerable;
                context.Args.Column = WpfHelpers.CreateComboboxColumnWithStaticItemsSource(
                    value, context.Property.Name);
            }
            else
            {
                context.Args.Column = WpfHelpers.CreateComboboxColumnWithItemsSourcePerRow(
                    selectFromListAttr.CollectionPropertyName, context.Property.Name);
            }
        }
    }
}
