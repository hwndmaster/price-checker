using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace Genius.PriceChecker.UI.Forms.Behaviors
{
    public class DataGridAutomatedBehavior : Behavior<DataGrid>
    {
        private bool? _showOnlyBrowsable;

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
            var property = (PropertyDescriptor) e.PropertyDescriptor;

            _showOnlyBrowsable = _showOnlyBrowsable ?? property.ComponentType.GetCustomAttributes(false)
                .FirstOrDefault(x => x is ShowOnlyBrowsableAttribute b && b.OnlyBrowsable) != null;

            var browsable = property.Attributes.OfType<BrowsableAttribute>().FirstOrDefault();
            if (_showOnlyBrowsable.Value && browsable?.Browsable != true
                || !_showOnlyBrowsable.Value && browsable?.Browsable == false)
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == nameof(IHasDirtyFlag.IsDirty)
                || e.PropertyName == nameof(ISelectable.IsSelected)
                || e.PropertyName == nameof(INotifyDataErrorInfo.HasErrors)
                || property.Attributes.OfType<GroupByAttribute>().Any()
                || typeof(ICollection).IsAssignableFrom(property.PropertyType))
            {
                e.Cancel = true;
                return;
            }

            if (property.Attributes.OfType<ReadOnlyAttribute>().Any(x => x.IsReadOnly))
            {
                e.Column.IsReadOnly = true;
            }

            var index = property.Attributes.OfType<DisplayIndexAttribute>().FirstOrDefault()?.Index;
            if (index.HasValue)
            {
                e.Column.DisplayIndex = index.Value;
            }

            if (typeof(ICommand).IsAssignableFrom(property.PropertyType))
            {
                SetupColumnButton(e, property);
                return;
            }

            if (!AssociatedObject.IsReadOnly && !e.Column.IsReadOnly)
            {
                SetupColumnCombobox(e, property);
                SetupColumnValidation(e.Column, property);
            }

            SetupColumnFormatting(e.Column, property);
            SetupColumnConverter(e.Column, property);
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

        private void SetupColumnFormatting(DataGridColumn column, PropertyDescriptor property)
        {
            var format = property.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
            if (format == null)
            {
                return;
            }

            var rightAlignedStyle = new Style {
                TargetType = typeof(DataGridCell),
                BasedOn = (Style) AssociatedObject.FindResource("MahApps.Styles.DataGridCell")
            };
            rightAlignedStyle.Setters.Add(new Setter(DataGridCell.HorizontalAlignmentProperty, HorizontalAlignment.Right));

            (column as DataGridBoundColumn).Binding.StringFormat = format.DataFormatString;
            column.CellStyle = rightAlignedStyle;
        }

        private void SetupColumnConverter(DataGridColumn column, PropertyDescriptor property)
        {
            var converterAttr = property.Attributes.OfType<ValueConverterAttribute>().FirstOrDefault();
            if (converterAttr == null)
            {
                return;
            }

            var binding = (column as DataGridBoundColumn).Binding as Binding;
            binding.Converter = Activator.CreateInstance(converterAttr.ValueConverterType) as IValueConverter;
        }

        private void SetupColumnButton(DataGridAutoGeneratingColumnEventArgs args, PropertyDescriptor property)
        {
            var icon = property.Attributes.OfType<IconAttribute>().FirstOrDefault()?.Name;
            args.Column = WpfHelpers.CreateButtonColumn(property.Name, icon);
        }

        private void SetupColumnCombobox(DataGridAutoGeneratingColumnEventArgs args, PropertyDescriptor property)
        {
            var selectFromListAttr = property.Attributes.OfType<SelectFromListAttribute>().FirstOrDefault();
            if (selectFromListAttr == null)
            {
                return;
            }

            if (selectFromListAttr.FromOwnerContext)
            {
                var prop = AssociatedObject.DataContext.GetType().GetProperty(selectFromListAttr.CollectionPropertyName);
                var value = prop.GetValue(AssociatedObject.DataContext) as IEnumerable;
                args.Column = WpfHelpers.CreateComboboxColumnWithStaticItemsSource(
                    value, property.Name);
            }
            else
            {
                args.Column = WpfHelpers.CreateComboboxColumnWithItemsSourcePerRow(
                    selectFromListAttr.CollectionPropertyName, property.Name);
            }
        }

        private void SetupColumnValidation(DataGridColumn column, PropertyDescriptor property)
        {
            if (!(column is DataGridBoundColumn boundColumn))
            {
                return;
            }

            var columnBinding = boundColumn.Binding as Binding;
            if (columnBinding == null)
            {
                return;
            }

            //columnBinding.ValidatesOnDataErrors = true;
            columnBinding.ValidatesOnNotifyDataErrors = true;
            columnBinding.NotifyOnValidationError = true;
            boundColumn.ElementStyle = (Style)Application.Current.FindResource("ValidatableCellElementStyle");
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