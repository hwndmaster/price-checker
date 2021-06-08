using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class TrackerProductSourceViewModel : ViewModelBase
    {
        public TrackerProductSourceViewModel(IUserInteraction ui, ProductSource productSource, decimal? lastPrice)
        {
            Id = productSource?.Id ?? Guid.NewGuid();
            Agent = productSource?.AgentId;
            Argument = productSource?.AgentArgument;
            LastPrice = lastPrice;

            PropertiesAreInitialized = true;

            ShowInBrowserCommand = new ActionCommand(_ => {
                ui.ShowProductInBrowser(productSource);
            });
        }

        [Browsable(false)]
        public Guid Id { get; set; }

        [SelectFromList(nameof(TrackerProductViewModel.Agents), fromOwnerContext: true)]
        public string Agent
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
