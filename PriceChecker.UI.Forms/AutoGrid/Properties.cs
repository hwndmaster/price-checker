using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;

namespace Genius.PriceChecker.UI.Forms.AutoGrid
{
    public static class Properties
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(Properties),
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
            var properties = itemType.GetProperties();
            var groupByProps = properties
                .Where(x => x.GetCustomAttributes(false).OfType<GroupByAttribute>().Any())
                .ToList();
            var filterByProps = properties
                .Where(x => x.GetCustomAttributes(false).OfType<FilterByAttribute>().Any())
                .ToList();

            if (!groupByProps.Any() && !filterByProps.Any())
            {
                d.SetValue(DataGrid.ItemsSourceProperty, e.NewValue);
            }
            else
            {
                var collectionViewSource = new CollectionViewSource();
                collectionViewSource.Source = e.NewValue;

                SetupGrouping(groupByProps, collectionViewSource);
                SetupFiltering(d, filterByProps, collectionViewSource);

                d.SetValue(DataGrid.ItemsSourceProperty, collectionViewSource.View);
            }
        }

        private static void SetupGrouping(List<PropertyInfo> groupByProps, CollectionViewSource collectionViewSource)
        {
            foreach (var groupByProp in groupByProps)
            {
                collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(groupByProp.Name));
            }
        }

        private static void SetupFiltering(DependencyObject d, List<PropertyInfo> filterByProps,
            CollectionViewSource collectionViewSource)
        {
            var vm = ((FrameworkElement)d).DataContext as ViewModelBase;
            if (vm == null)
                throw new InvalidCastException($"Cannot cast DataContext to {nameof(ViewModelBase)}");

            var filterContext = vm.GetType().GetProperties()
                .FirstOrDefault(x => x.GetCustomAttributes(false).OfType<FilterContextAttribute>().Any());

            if (filterContext == null || !filterByProps.Any())
                return;

            string filter = string.Empty;
            vm.WhenChanged(filterContext.Name, (string s) => {
                filter = s;
                collectionViewSource.View.Refresh();
            });

            collectionViewSource.Filter += (object sender, FilterEventArgs e) =>
            {
                if (string.IsNullOrEmpty(filter))
                {
                    e.Accepted = true;
                    return;
                }

                foreach (var filterProp in filterByProps)
                {
                    var value = filterProp.GetValue(e.Item);
                    if (value is string stringValue)
                    {
                        if (stringValue.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                        {
                            e.Accepted = true;
                            return;
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Type {value.GetType().Name} is not suppoerted yet for AutoGrid filtering");
                    }
                }

                e.Accepted = false;
            };
        }
    }
}
