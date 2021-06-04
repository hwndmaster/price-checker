using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace Genius.PriceChecker.UI.Forms.AutoGrid
{
    public class AttachingBehavior : Behavior<DataGrid>
    {
        private bool? _showOnlyBrowsable;

        private static IAutoGridColumnBehavior[] _columnBehaviors;

        static AttachingBehavior()
        {
            _columnBehaviors = new IAutoGridColumnBehavior[] {
                new ColumnReadOnlyBehavior(),
                new ColumnDisplayIndexBehavior(),
                new ColumnButtonBehavior(),
                new ColumnWithImageBehavior(),
                new ColumnComboboxBehavior(),
                new ColumnValidationBehavior(),
                new ColumnConverterBehavior(),
                new ColumnFormattingBehavior(),
                new ColumnTooltipBehavior(),
                new ColumnStylingBehavior(),
                new ColumnHeaderNameBehavior()
            };
        }

        protected override void OnAttached()
        {
            AssociatedObject.AutoGenerateColumns = true;
            AssociatedObject.AutoGeneratingColumn += OnAutoGeneratingColumn;

            var dpd = DependencyPropertyDescriptor.FromProperty(DataGrid.ItemsSourceProperty, typeof(DataGrid));
            if (dpd != null)
            {
                dpd.AddValueChanged(AssociatedObject, OnItemsSourceChanged);
            }

            base.OnAttached();
        }

        private void OnItemsSourceChanged(object sender, EventArgs e)
        {
            if (AssociatedObject.ItemsSource == null)
            {
                return;
            }

            if (AssociatedObject.SelectionMode == DataGridSelectionMode.Extended &&
                typeof(ISelectable).IsAssignableFrom(GetItemType()))
            {
                BindIsSelected();
            }
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var context = new AutoGridColumnContext(AssociatedObject, e, (PropertyDescriptor) e.PropertyDescriptor);

            if (!IsBrowsable(context))
            {
                e.Cancel = true;
                return;
            }

            var ignoreProperties = new [] {
                nameof(IHasDirtyFlag.IsDirty),
                nameof(ISelectable.IsSelected),
                nameof(INotifyDataErrorInfo.HasErrors)
            };

            if (ignoreProperties.Contains(e.PropertyName)
                || context.GetAttribute<GroupByAttribute>() != null
                || typeof(ICollection).IsAssignableFrom(context.Property.PropertyType))
            {
                e.Cancel = true;
                return;
            }

            foreach (var columnBehavior in _columnBehaviors)
            {
                columnBehavior.Attach(context);
            }
        }

        private bool IsBrowsable(AutoGridColumnContext context)
        {
            _showOnlyBrowsable = _showOnlyBrowsable ?? context.Property.ComponentType.GetCustomAttributes(false)
                .FirstOrDefault(x => x is ShowOnlyBrowsableAttribute b && b.OnlyBrowsable) != null;

            var browsable = context.GetAttribute<BrowsableAttribute>();
            if (_showOnlyBrowsable.Value && browsable?.Browsable != true
                || !_showOnlyBrowsable.Value && browsable?.Browsable == false)
            {
                return false;
            }

            return true;
        }

        private void BindIsSelected()
        {
            var binding = new Binding(nameof(ISelectable.IsSelected));
            var rowStyle = new Style {
                TargetType = typeof(DataGridRow),
                BasedOn = (Style) AssociatedObject.FindResource("MahApps.Styles.DataGridRow")
            };
            rowStyle.Setters.Add(new Setter(DataGrid.IsSelectedProperty, binding));
            AssociatedObject.RowStyle = rowStyle;
        }

        private Type GetItemType()
        {
            var sourceCollection = AssociatedObject.ItemsSource;
            if (sourceCollection is ListCollectionView listCollectionView)
            {
                sourceCollection = listCollectionView.SourceCollection;
            }
            return sourceCollection.GetType().GetGenericArguments().Single();
        }
    }
}
