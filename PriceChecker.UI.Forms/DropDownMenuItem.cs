using System.Windows.Input;

namespace Genius.PriceChecker.UI.Forms
{
    public class DropDownMenuItem
    {
        public DropDownMenuItem(string name, ICommand command)
        {
            Name = name;
            Command = command;
        }

        public string Name { get; set; }
        public ICommand Command { get; set; }
    }
}
