using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media.Imaging;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels;

internal sealed class TrackerProductSourceViewModel : ViewModelBase
{
    public TrackerProductSourceViewModel(IProductInteraction productInteraction, ProductSource? productSource, decimal? lastPrice)
    {
        InitializeProperties(() => {
            Id = productSource?.Id ?? Guid.NewGuid();
            AgentKey = productSource?.AgentKey ?? string.Empty;
            Argument = productSource?.AgentArgument ?? string.Empty;
            LastPrice = lastPrice;
        });

        ShowInBrowserCommand = new ActionCommand(_ =>
            productInteraction.ShowProductInBrowser(productSource));
    }

    [Browsable(false)]
    public Guid Id { get; set; }

    [SelectFromList(nameof(TrackerProductViewModel.Agents), fromOwnerContext: true)]
    public string AgentKey
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    [Greedy]
    public string Argument
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    [ReadOnly(true)]
    [DisplayFormat(DataFormatString = "€ #,##0.00")]
    [Style(HorizontalAlignment = HorizontalAlignment.Right)]
    public decimal? LastPrice
    {
        get => GetOrDefault<decimal?>();
        set => RaiseAndSetIfChanged(value);
    }

    [ReadOnly(true)]
    [IconSource(nameof(StatusIcon), 16d)]
    public AgentHandlingStatus Status
    {
        get => GetOrDefault(AgentHandlingStatus.Unknown);
        set => RaiseAndSetIfChanged(value, (_, __) => OnPropertyChanged(nameof(StatusIcon)));
    }

    [Browsable(false)]
    public BitmapImage? StatusIcon
    {
        get
        {
            var icon = Status switch
            {
                AgentHandlingStatus.Unknown => "Unknown16",
                AgentHandlingStatus.Success => "DonePink16",
                AgentHandlingStatus.CouldNotFetch => "Error16",
                AgentHandlingStatus.CouldNotMatch => "Error16",
                AgentHandlingStatus.CouldNotParse => "Error16",
                AgentHandlingStatus.InvalidPrice => "Warning16",
                {} => null
            };
            if (icon is null)
                return null;
            return (BitmapImage)App.Current.FindResource(icon);
        }
    }

    [Icon("Trash16")]
    public IActionCommand DeleteCommand { get; } = new ActionCommand();

    [Browsable(true)]
    [Icon("Web16")]
    public IActionCommand ShowInBrowserCommand { get; }
}
