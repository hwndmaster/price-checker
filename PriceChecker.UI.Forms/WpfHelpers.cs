using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using MahApps.Metro.Controls;

namespace Genius.PriceChecker.UI.Forms
{
    [ExcludeFromCodeCoverage]
    public static class WpfHelpers
    {
        public static void AddFlyout<T>(FrameworkElement owner, string isOpenBindingPath, string sourcePath = null)
            where T: Flyout, new()
        {
            var parentWindow = Window.GetWindow(owner);
            object obj = parentWindow.FindName("flyoutsControl");
            var flyout = (FlyoutsControl) obj;
            var child = new T();
            if (sourcePath == null)
            {
                child.DataContext = owner.DataContext;
            }
            else
            {
                BindingOperations.SetBinding(child, Flyout.DataContextProperty,
                    new Binding(sourcePath) { Source = owner.DataContext });
                //: TypeDescriptor.GetProperties(owner.DataContext).Find(sourcePath, false).GetValue(owner.DataContext);
            }
            BindingOperations.SetBinding(child, Flyout.IsOpenProperty, new Binding(isOpenBindingPath) { Source = owner.DataContext });
            (flyout as IAddChild).AddChild(child);
        }

        public static DataGridTemplateColumn CreateButtonColumn(string commandPath, string iconName)
        {
            var caption = commandPath.Replace("Command", "");

            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetBinding(Button.CommandProperty, new Binding(commandPath));
            buttonFactory.SetValue(Button.ToolTipProperty, caption);
            buttonFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            if (iconName != null)
            {
                var imageFactory = new FrameworkElementFactory(typeof(Image));
                imageFactory.SetValue(Image.SourceProperty, Application.Current.FindResource(iconName));
                buttonFactory.AppendChild(imageFactory);
            }
            else
            {
                buttonFactory.SetValue(Button.ContentProperty, caption);
            }

            var column = new DataGridTemplateColumn();
            column.CellTemplate = new DataTemplate { VisualTree = buttonFactory };
            return column;
        }

        public static DataGridComboBoxColumn CreateComboboxColumnWithStaticItemsSource(IEnumerable itemsSource, string valuePath)
        {
            var column = new DataGridComboBoxColumn();
            column.Header = valuePath;
            column.ItemsSource = itemsSource;
            column.SelectedValueBinding = new Binding(valuePath);
            return column;
        }

        public static DataGridTemplateColumn CreateComboboxColumnWithItemsSourcePerRow(string itemsSourcePath, string valuePath)
        {
            var column = new DataGridTemplateColumn();
            column.Header = valuePath;

            var bindToValue = new Binding(valuePath);
            var bindToItemsSource = new Binding(itemsSourcePath);

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, bindToValue);
            var textTemplate = new DataTemplate();
            textTemplate.VisualTree = textFactory;

            var comboFactory = new FrameworkElementFactory(typeof(ComboBox));
            comboFactory.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
            comboFactory.SetBinding(ComboBox.SelectedItemProperty, bindToValue);
            comboFactory.SetBinding(ComboBox.ItemsSourceProperty, bindToItemsSource);

            var comboTemplate = new DataTemplate { VisualTree = comboFactory };

            column.CellTemplate = textTemplate;
            column.CellEditingTemplate = comboTemplate;

            return column;
        }

        public static Style EnsureDefaultCellStyle(DataGridColumn column)
        {
            if (column.CellStyle == null)
            {
                column.CellStyle = new Style {
                    TargetType = typeof(DataGridCell),
                    BasedOn = (Style) Application.Current.FindResource("MahApps.Styles.DataGridCell")
                };
            }

            return column.CellStyle;
        }

        public static void SetCellHorizontalAlignment(DataGridColumn column, HorizontalAlignment alignment)
        {
            EnsureDefaultCellStyle(column);

            column.CellStyle.Setters.Add(new Setter(DataGridCell.HorizontalAlignmentProperty, alignment));
        }
    }
}
