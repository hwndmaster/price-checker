using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
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
        private readonly IAgentQueryService _agentQuery;
        private readonly IProductQueryService _productQuery;
        private readonly IProductStatusProvider _statusProvider;
        private readonly ICommandBus _commandBus;
        private readonly IUserInteraction _ui;

        private Product _product;

        public TrackerProductViewModel(Product product, IEventBus eventBus,
            ICommandBus commandBus,
            IAgentQueryService agentQuery,
            IProductQueryService productQuery, IProductStatusProvider statusProvider,
            IUserInteraction ui)
        {
            _agentQuery = agentQuery;
            _productQuery = productQuery;
            _statusProvider = statusProvider;
            _commandBus = commandBus;
            _product = product;
            _ui = ui;

            InitializeProperties(() =>
            {
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

            ResetCommand = new ActionCommand(_ => ResetForm(), _ => _product != null);

            DropPricesCommand = new ActionCommand(async _ =>
            {
                if (!_ui.AskForConfirmation("Are you sure?", "Prices drop confirmation"))
                    return;
                await commandBus.SendAsync(new ProductDropPricesCommand(_product.Id));
                Reconcile(true);
            });

            RefreshPriceCommand = new ActionCommand(async _ =>
            {
                if (Status == ProductScanStatus.Scanning)
                    return;
                await commandBus.SendAsync(new ProductEnqueueScanCommand(product.Id));
            }, _ => Status != ProductScanStatus.Scanning);

            eventBus.WhenFired<AgentsAffectedEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                    RefreshAgents()
                );
            eventBus.WhenFired<ProductsAffectedEvent>()
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
            RecentPrice = _product.Recent.Any()
                ? _product.Recent.Min(x => x.Price)
                : null;
            Status = lowestPriceUpdated && _product.Lowest != null ?
                ProductScanStatus.ScannedNewLowest :
                _statusProvider.DetermineStatus(_product);
            LastUpdated = null;

            if (lowestPriceUpdated && LowestPrice.HasValue && previousLowestPrice.HasValue)
            {
                var x = (1 - LowestPrice.Value / previousLowestPrice) * 100;
                StatusText = $"The new price is by {x:0}% less than by previous scan ({LowestPrice.Value:#,##0.00} vs {previousLowestPrice.Value:#,##0.00})";
            }
            else if (Status == ProductScanStatus.ScannedWithErrors)
            {
                StatusText = "One or more source hasn't updated its price. Check the logs.";
            }
            else
            {
                StatusText = string.Empty;
            }

            if (_product.Recent.Any())
            {
                LastUpdated = _product.Recent.Max(x => x.FoundDate);

                var pricesDict = _product.Recent.ToDictionary(x => x.ProductSourceId, x => x.Price);
                foreach (var source in Sources)
                {
                    source.LastPrice = pricesDict.ContainsKey(source.Id) ?
                        pricesDict[source.Id] :
                        null;
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

        private async Task CommitProduct()
        {
            if (string.IsNullOrEmpty(Name))
            {
                _ui.ShowWarning("Product name cannot be empty.");
                return;
            }

            var sources = Sources.Select(x => new ProductSource
            {
                Id = x.Id,
                    AgentKey = x.AgentKey,
                    AgentArgument = x.Argument
            }).ToArray();

            if (_product == null)
            {
                var productId = await _commandBus.SendAsync(new ProductCreateCommand(Name, Category, Description, sources));
                _product = _productQuery.FindById(productId);
            }
            else
            {
                await _commandBus.SendAsync(new ProductUpdateCommand(_product.Id, Name, Category, Description, sources));
            }
        }

        private TrackerProductSourceViewModel CreateSourceViewModel(ProductSource productSource)
        {
            var lastPrice = productSource == null || _product?.Recent == null ?
                null :
                _product.Recent.FirstOrDefault(x => x.ProductSourceId == productSource.Id)?.Price;
            var vm = new TrackerProductSourceViewModel(_ui, productSource, lastPrice);
            vm.DeleteCommand.Executed.Subscribe(_ =>
                Sources.Remove(vm));
            return vm;
        }

        private void RefreshAgents()
        {
            Agents = _agentQuery.GetAll().Select(x => x.Key).ToList();
        }

        private void RefreshCategories()
        {
            Categories.ReplaceItems(
                _productQuery.GetAll().Select(x => x.Category).Distinct());
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
        [IconSource(nameof(StatusIcon), fixedSize : 16d, hideText : true)]
        [TooltipSource(nameof(StatusText))]
        [Style(HorizontalAlignment = HorizontalAlignment.Right)]
        public ProductScanStatus Status
        {
            get => GetOrDefault<ProductScanStatus>();
            set => RaiseAndSetIfChanged(value, (_, __) =>
                OnPropertyChanged(nameof(StatusIcon)));
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
        [DisplayFormat(DataFormatString = "€ #,##0.00")]
        [Style(HorizontalAlignment = HorizontalAlignment.Right)]
        public decimal? RecentPrice
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
