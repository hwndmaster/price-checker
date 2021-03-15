using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
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

        public static DataGridTemplateColumn CreateColumnWithImage(string valuePath, string imageName, string imageTooltip = null, string imageVisibilityFlagPath = null, double? fixedSize = null, bool imageIsPath = false)
        {
            var bindToValue = new Binding(valuePath);
            var column = new DataGridTemplateColumn {
                Header = valuePath,
                SortMemberPath = valuePath
            };

            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var imageFactory = new FrameworkElementFactory(typeof(Image));

            if (imageIsPath)
                imageFactory.SetBinding(Image.SourceProperty, new Binding(imageName));
            else
                imageFactory.SetValue(Image.SourceProperty, (BitmapImage)Application.Current.FindResource(imageName));

            if (imageTooltip != null)
                imageFactory.SetValue(Image.ToolTipProperty, imageTooltip);

            if (imageVisibilityFlagPath != null)
                imageFactory.SetValue(Image.VisibilityProperty, new Binding(imageVisibilityFlagPath) { Converter = new BooleanToVisibilityConverter() });

            if (fixedSize != null)
            {
                imageFactory.SetValue(Image.HeightProperty, fixedSize.Value);
                imageFactory.SetValue(Image.WidthProperty, fixedSize.Value);
            }

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, bindToValue);
            textFactory.SetValue(Image.MarginProperty, new Thickness(5, 0, 0, 0));

            stackPanelFactory.AppendChild(imageFactory);
            stackPanelFactory.AppendChild(textFactory);

            column.CellTemplate = new DataTemplate { VisualTree = stackPanelFactory };

            return column;
        }
    }
}
