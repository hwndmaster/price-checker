using System.Text.RegularExpressions;

namespace Genius.PriceChecker.UI.Forms.AutoGrid.Behaviors
{
    public class ColumnHeaderNameBehavior : IAutoGridColumnBehavior
    {
        public void Attach(AutoGridColumnContext context)
        {
            if (context.Args.Column.Header is not string headerText)
            {
                return;
            }

            context.Args.Column.Header = Regex.Replace(headerText, "[A-Z]", " $0");
        }
    }
}
