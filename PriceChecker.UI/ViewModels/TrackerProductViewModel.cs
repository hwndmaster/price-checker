using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

namespace Genius.PriceChecker.UI.ViewModels
{
    [ShowOnlyBrowsable(true)]
    public class TrackerProductViewModel : ViewModelBase<TrackerProductViewModel>, ISelectable
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IProductRepository _productRepo;
        private readonly IProductStatusProvider _statusProvider;
        private readonly IUserInteraction _ui;

        private Product _product;

        public TrackerProductViewModel(Product product, IEventBus eventBus,
            IAgentRepository agentRepo, IProductRepository productRepo,
            IProductStatusProvider statusProvider, IUserInteraction ui)
        {
            _agentRepo = agentRepo;
            _productRepo = productRepo;
            _statusProvider = statusProvider;
            _product = product;
            _ui = ui;

            RefreshAgents();
            RefreshCategories();

            if (_product != null)
            {
                ResetForm();
                Reconcile(false);
            }

            CommitProductCommand = new ActionCommand(_ => CommitProduct());

            ShowInBrowserCommand = new ActionCommand(_ => {
                if (_product.Lowest == null)
                    return;

                ui.ShowProductInBrowser(_product, _product.Lowest.AgentId);
            });

            AddSourceCommand = new ActionCommand(_ => {
                Sources.Add(CreateSourceViewModel(null));
            });

            ResetCommand = new ActionCommand(_ => {
                ResetForm();
            }, _ => _product != null);

            eventBus.Subscribe<AgentsUpdatedEvent, AgentDeletedEvent>(() => {
                RefreshAgents();
            });
            eventBus.Subscribe<ProductUpdatedEvent, ProductAddedEvent>(() => {
                RefreshCategories();
            });

            PropertiesAreInitialized = true;
        }

        public void Reconcile(bool lowestPriceUpdated)
        {
            LowestPrice = _product.Lowest?.Price;
            LowestFoundOn = _product.Lowest?.FoundDate;
            Status = lowestPriceUpdated
                ? ProductScanStatus.ScannedNewLowest
                : _statusProvider.DetermineStatus(_product);

            if (_product.Recent.Any())
            {
                LastUpdated = _product.Recent.Max(x => x.FoundDate);
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
                AgentId = x.Agent, AgentArgument = x.Argument }).ToArray();

            _productRepo.Store(_product);
        }

        private TrackerProductSourceViewModel CreateSourceViewModel(ProductSource productSource)
        {
            var vm = new TrackerProductSourceViewModel(productSource);
            vm.DeleteCommand.Executed += (_, __) => {
                Sources.Remove(vm);
            };
            return vm;
        }

        private void RefreshAgents()
        {
            Agents = _agentRepo.GetAll().Select(x => x.Id).ToList();
        }

        private void RefreshCategories()
        {
            Categories.ReplaceItems(_productRepo.GetAll().Select(x => x.Category).Distinct());
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

        public BitmapImage StatusIcon
        {
            get
            {
                var icon = Status switch
                {
                    ProductScanStatus.NotScanned => "Unknown16",
                    ProductScanStatus.Scanning => "Loading32",
                    ProductScanStatus.ScannedOk => "DonePink16",
                    ProductScanStatus.ScannedNewLowest => "Dance32",
                    ProductScanStatus.Outdated => "Outdated16",
                    ProductScanStatus.Failed => "Error16",
                    {} => null
                };
                if (icon == null)
                    return null;
                return (BitmapImage)App.Current.FindResource(icon);
            }
        }

        public string StatusText
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [Browsable(true)]
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
    }
}
