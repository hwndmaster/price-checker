using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ValueConverters;
using ReactiveUI;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface ITrackerProductViewModel : IViewModel, ISelectable
    {
        void Reconcile(bool lowestPriceUpdated);
        void SetFailed(string errorMessage);

        Guid Id { get; }
        string Name { get; }
        ProductScanStatus Status { get; set; }
        IActionCommand CommitProductCommand { get; }
        IActionCommand RefreshPriceCommand { get; }
    }

    [ShowOnlyBrowsable(true)]
    internal sealed class TrackerProductViewModel : ViewModelBase, ITrackerProductViewModel
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IProductPriceManager _productMng;
        private readonly IProductRepository _productRepo;
        private readonly IProductStatusProvider _statusProvider;
        private readonly IUserInteraction _ui;

        private Product _product;

        public TrackerProductViewModel(Product product, IEventBus eventBus,
            IAgentRepository agentRepo, IProductPriceManager productMng,
            IProductRepository productRepo, IProductStatusProvider statusProvider,
            IUserInteraction ui)
        {
            _agentRepo = agentRepo;
            _productMng = productMng;
            _productRepo = productRepo;
            _statusProvider = statusProvider;
            _product = product;
            _ui = ui;

            InitializeProperties(() => {
                RefreshAgents();
                RefreshCategories();

                if (_product != null)
                {
                    ResetForm();
                    Reconcile(false);
                }
            });

            CommitProductCommand = new ActionCommand(_ => CommitProduct());

            ShowInBrowserCommand = new ActionCommand(_ =>
                ui.ShowProductInBrowser(_product.Lowest?.ProductSource));

            AddSourceCommand = new ActionCommand(_ =>
                Sources.Add(CreateSourceViewModel(null)));

            ResetCommand = new ActionCommand(_ => {
                ResetForm();
            }, _ => _product != null);

            DropPricesCommand = new ActionCommand(_ => {
                if (!_ui.AskForConfirmation("Are you sure?", "Prices drop confirmation"))
                    return;
                _productRepo.DropPrices(_product);
                Reconcile(true);
            });

            RefreshPriceCommand = new ActionCommand(_ => {
                if (Status == ProductScanStatus.Scanning)
                    return;
                _productMng.EnqueueScan(product.Id);
            }, _ => Status != ProductScanStatus.Scanning);

            eventBus.WhenFired<AgentsUpdatedEvent, AgentDeletedEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                    RefreshAgents()
                );
            eventBus.WhenFired<ProductUpdatedEvent, ProductAddedEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                    RefreshCategories()
                );
        }

        public void Reconcile(bool lowestPriceUpdated)
        {
            var previousLowestPrice = LowestPrice;
            LowestPrice = _product.Lowest?.Price;
            LowestFoundOn = _product.Lowest?.FoundDate;
            Status = lowestPriceUpdated && _product.Lowest != null
                ? ProductScanStatus.ScannedNewLowest
                : _statusProvider.DetermineStatus(_product);
            LastUpdated = null;

            if (lowestPriceUpdated && LowestPrice.HasValue && previousLowestPrice.HasValue)
            {
                var x = (1 - LowestPrice.Value/previousLowestPrice) * 100;
                StatusText = $"The new price is by {x:0}% less than by previous scan ({LowestPrice.Value:#,##0.00} vs {previousLowestPrice.Value:#,##0.00})";
            }
            else if (Status == ProductScanStatus.ScannedWithErrors)
                StatusText = "One or more source hasn't updated its price. Check the logs.";
            else
                StatusText = string.Empty;

            if (_product.Recent.Any())
            {
                LastUpdated = _product.Recent.Max(x => x.FoundDate);

                var pricesDict = _product.Recent.ToDictionary(x => x.ProductSourceId, x => x.Price);
                foreach (var source in Sources)
                {
                    source.LastPrice = pricesDict.ContainsKey(source.Id)
                        ? pricesDict[source.Id]
                        : null;
                }
            }
            else
            {
                foreach (var source in Sources)
                {
                    source.LastPrice = null;
                }
            }
        }

        public void SetFailed(string errorMessage)
        {
            Status = ProductScanStatus.Failed;
            StatusText = errorMessage;
        }

        private void CommitProduct()
        {
            if (string.IsNullOrEmpty(Name))
            {
                _ui.ShowWarning("Product name cannot be empty.");
                return;
            }

            _product = _product ?? new Product();
            _product.Name = Name;
            _product.Category = Category;
            _product.Description = Description;

            _product.Sources = Sources.Select(x => new ProductSource {
                Id = x.Id,
                AgentId = x.Agent,
                AgentArgument = x.Argument
            }).ToArray();

            _productRepo.Store(_product);
        }

        private TrackerProductSourceViewModel CreateSourceViewModel(ProductSource productSource)
        {
            var lastPrice = productSource == null || _product?.Recent == null
                ? null
                : _product.Recent.FirstOrDefault(x => x.ProductSourceId == productSource.Id)?.Price;
            var vm = new TrackerProductSourceViewModel(_ui, productSource, lastPrice);
            vm.DeleteCommand.Executed.Subscribe(_ =>
                Sources.Remove(vm));
            return vm;
        }

        private void RefreshAgents()
        {
            Agents = _agentRepo.GetAll().Select(x => x.Id).ToList();
        }

        private void RefreshCategories()
        {
            Categories.ReplaceItems(
                _productRepo.GetAll().Select(x => x.Category).Distinct());
        }

        private void ResetForm()
        {
            Name = _product.Name;
            Category = _product.Category;
            Description = _product.Description;

            var productSourceVms = _product.Sources.Select(x => CreateSourceViewModel(x));
            Sources.ReplaceItems(productSourceVms);
        }

        public Guid Id => _product.Id;

        public IReadOnlyCollection<string> Agents { get; private set; }

        public ObservableCollection<TrackerProductSourceViewModel> Sources { get; } = new ObservableCollection<TrackerProductSourceViewModel>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();

        [Browsable(true)]
        [IconSource(nameof(StatusIcon), fixedSize: 16d, hideText: true)]
        [TooltipSource(nameof(StatusText))]
        [Style(HorizontalAlignment = HorizontalAlignment.Right)]
        public ProductScanStatus Status
        {
            get => GetOrDefault<ProductScanStatus>();
            set => RaiseAndSetIfChanged(value, (@old, @new) => {
                OnPropertyChanged(nameof(StatusIcon));
            });
        }

        public BitmapImage StatusIcon => ResourcesHelper.GetStatusIcon(Status);

        public string StatusText
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [Browsable(true)]
        [FilterBy]
        public string Name
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [GroupBy]
        public string Category
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        public string Description
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [Browsable(true)]
        [DisplayFormat(DataFormatString = "€ #,##0.00")]
        [Style(HorizontalAlignment = HorizontalAlignment.Right)]
        public decimal? LowestPrice
        {
            get => GetOrDefault<decimal?>();
            set => RaiseAndSetIfChanged(value);
        }

        [Browsable(true)]
        [ValueConverter(typeof(DateTimeToHumanizedConverter))]
        public DateTime? LowestFoundOn
        {
            get => GetOrDefault<DateTime?>();
            set => RaiseAndSetIfChanged(value);
        }

        [Browsable(true)]
        [ValueConverter(typeof(DateTimeToHumanizedConverter))]
        public DateTime? LastUpdated
        {
            get => GetOrDefault<DateTime?>();
            set => RaiseAndSetIfChanged(value);
        }

        public bool IsSelected
        {
            get => GetOrDefault<bool>();
            set => RaiseAndSetIfChanged(value);
        }

        public IActionCommand CommitProductCommand { get; }

        [Browsable(true)]
        [Icon("Web16")]
        public IActionCommand ShowInBrowserCommand { get; }
        public IActionCommand AddSourceCommand { get; }
        public IActionCommand ResetCommand { get; }
        public IActionCommand DropPricesCommand { get; }

        [Browsable(true)]
        [Icon("Refresh16")]
        public IActionCommand RefreshPriceCommand { get; }
    }
}
