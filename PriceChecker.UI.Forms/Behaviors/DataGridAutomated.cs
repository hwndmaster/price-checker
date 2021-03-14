using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Genius.PriceChecker.UI.Forms.Attributes;

namespace Genius.PriceChecker.UI.Forms.Behaviors
{
    public static class DataGridAutomated
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(DataGridAutomated),
            new PropertyMetadata(ItemsSourceChanged)
        );

        public static void SetItemsSource(DependencyObject element, IEnumerable value)
        {
            element.SetValue(ItemsSourceProperty, value);
        }

        public static IEnumerable GetItemsSource(DependencyObject element)
        {
            return (IEnumerable) element.GetValue(ItemsSourceProperty);
        }

        public static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemType = e.NewValue.GetType().GetGenericArguments().Single();
            var groupByProps = itemType.GetProperties()
                .Where(x => x.GetCustomAttributes(false).OfType<GroupByAttribute>().Any())
                .ToList();

            if (!groupByProps.Any())
            {
                d.SetValue(DataGrid.ItemsSourceProperty, e.NewValue);
            }
            else
            {
                var collectionViewSource = new CollectionViewSource();
                collectionViewSource.Source = e.NewValue;

                foreach (var groupByProp in groupByProps)
                {
                    collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(groupByProp.Name));
                }

                d.SetValue(DataGrid.ItemsSourceProperty, collectionViewSource.View);
            }
        }
    }
}
