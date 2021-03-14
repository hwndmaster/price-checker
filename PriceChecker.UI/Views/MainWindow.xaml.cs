using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.UI.ViewModels;
using MahApps.Metro.Controls;

namespace Genius.PriceChecker.UI.Views
{
    [ExcludeFromCodeCoverage]
    public partial class MainWindow : MetroWindow
    {
        public MainWindow(MainViewModel mainVm)
        {
            InitializeComponent();

            DataContext = mainVm;
        }
    }
}
