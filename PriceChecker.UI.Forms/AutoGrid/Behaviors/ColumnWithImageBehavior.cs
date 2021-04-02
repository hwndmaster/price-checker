using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Genius.PriceChecker.UI.Forms.Attributes;
using WpfAnimatedGif;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnWithImageBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            var iconAttr = context.GetAttribute<IconSourceAttribute>();
            if (iconAttr == null)
            {
                return;
            }

            context.Args.Column = CreateColumnWithImage(context.Property.Name,
                iconAttr.IconPropertyPath, fixedSize: iconAttr.FixedSize,
                imageIsPath: true, hideText: iconAttr.HideText);
        }

        private static DataGridTemplateColumn CreateColumnWithImage(string valuePath,
            string imageName, string imageTooltip = null,
            string imageVisibilityFlagPath = null,
            double? fixedSize = null, bool imageIsPath = false,
            bool hideText = false)
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
                imageFactory.SetBinding(ImageBehavior.AnimatedSourceProperty, new Binding(imageName));
            else
                imageFactory.SetValue(ImageBehavior.AnimatedSourceProperty, (BitmapImage)Application.Current.FindResource(imageName));

            if (imageTooltip != null)
                imageFactory.SetValue(Image.ToolTipProperty, imageTooltip);

            if (imageVisibilityFlagPath != null)
                imageFactory.SetValue(Image.VisibilityProperty, new Binding(imageVisibilityFlagPath) { Converter = new BooleanToVisibilityConverter() });

            if (fixedSize != null)
            {
                imageFactory.SetValue(Image.HeightProperty, fixedSize.Value);
                imageFactory.SetValue(Image.WidthProperty, fixedSize.Value);
            }

            stackPanelFactory.AppendChild(imageFactory);

            if (!hideText)
            {
                var textFactory = new FrameworkElementFactory(typeof(TextBlock));
                textFactory.SetBinding(TextBlock.TextProperty, bindToValue);
                textFactory.SetValue(Image.MarginProperty, new Thickness(5, 0, 0, 0));
                stackPanelFactory.AppendChild(textFactory);
            }

            column.CellTemplate = new DataTemplate { VisualTree = stackPanelFactory };

            return column;
        }
    }
}
