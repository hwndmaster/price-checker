using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Genius.PriceChecker.Core.Models;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    internal sealed class TrackerProductSourceViewModel : ViewModelBase
    {
        public TrackerProductSourceViewModel(IUserInteraction ui, ProductSource productSource, decimal? lastPrice)
        {
            InitializeProperties(() => {
                Id = productSource?.Id ?? Guid.NewGuid();
                AgentKey = productSource?.AgentKey;
                Argument = productSource?.AgentArgument;
                LastPrice = lastPrice;
            });

            ShowInBrowserCommand = new ActionCommand(_ =>
                ui.ShowProductInBrowser(productSource));
        }

        [Browsable(false)]
        public Guid Id { get; set; }

        [SelectFromList(nameof(TrackerProductViewModel.Agents), fromOwnerContext: true)]
        public string AgentKey
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        public string Argument
        {
            get => GetOrDefault<string>();
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

        [Icon("Trash16")]
        public IActionCommand DeleteCommand { get; } = new ActionCommand();

        [Browsable(true)]
        [Icon("Web16")]
        public IActionCommand ShowInBrowserCommand { get; }
    }
}
