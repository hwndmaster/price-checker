using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.Attributes;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using ReactiveUI;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface ITrackerViewModel : ITabViewModel
    { }

    internal sealed class TrackerViewModel : TabViewModelBase, ITrackerViewModel
    {
        private readonly IEventBus _eventBus;
        private readonly IProductQueryService _productQuery;
        private readonly IViewModelFactory _vmFactory;
        private readonly ITrackerScanContext _scanContext;

        public TrackerViewModel(IEventBus eventBus,
            IProductQueryService productQuery,
            IViewModelFactory vmFactory,
            IUserInteraction ui,
            ITrackerScanContext scanContext,
            ICommandBus commandBus)
        {
            _eventBus = eventBus;
            _productQuery = productQuery;
            _vmFactory = vmFactory;
            _scanContext = scanContext;

            RefreshAllCommand = new ActionCommand(_ => {
                IsAddEditProductVisible = false;
                EnqueueScan(Products);
            });
            RefreshSelectedCommand = new ActionCommand(_ => {
                IsAddEditProductVisible = false;
                EnqueueScan(Products.Where(x => x.IsSelected).ToArray());
            });
            OpenAddProductFlyoutCommand = new ActionCommand(_ => {
                IsAddEditProductVisible = !IsAddEditProductVisible;
                if (IsAddEditProductVisible)
                {
                    EditingProduct = vmFactory.CreateTrackerProduct(null);
                    EditingProduct.CommitProductCommand.Executed
                        .Take(1)
                        .Subscribe(_ => {
                            IsAddEditProductVisible = false;
                            ReloadList();
                        });
                }
            });
            OpenEditProductFlyoutCommand = new ActionCommand(_ => {
                EditingProduct = Products.FirstOrDefault(x => x.IsSelected);
                EditingProduct?.CommitProductCommand.Executed
                    .Take(1)
                    .Subscribe(_ => IsAddEditProductVisible = false);
                IsAddEditProductVisible = EditingProduct != null;
            });

            DeleteProductCommand = new ActionCommand(async _ => {
                IsAddEditProductVisible = false;
                var selectedProduct = Products.FirstOrDefault(x => x.IsSelected);
                if (selectedProduct == null)
                {
                    ui.ShowWarning("No product selected.");
                    return;
                }
                if (!ui.AskForConfirmation($"Are you sure you want to delete the selected '{selectedProduct.Name}' product?", "Delete product"))
                    return;

                Products.Remove(selectedProduct);
                await commandBus.SendAsync(new ProductDeleteCommand(selectedProduct.Id));
            });

            _eventBus.WhenFired<ProductScanStartedEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ev =>
                    Products.First(x => x.Id == ev.Product.Id).Status = Core.Models.ProductScanStatus.Scanning
                );
            _eventBus.WhenFired<ProductScannedEvent>()
                .Subscribe(ev =>
                    Products.First(x => x.Id == ev.Product.Id).Reconcile(ev.LowestPriceUpdated));
            _eventBus.WhenFired<ProductScanFailedEvent>()
                .Subscribe(ev =>
                    Products.First(x => x.Id == ev.Product.Id).SetFailed(ev.ErrorMessage));

            Deactivated.Executed.Subscribe(_ =>
                IsAddEditProductVisible = false);

            RefreshOptions = new List<DropDownMenuItem> {
                new DropDownMenuItem("Refresh all", RefreshAllCommand),
                new DropDownMenuItem("Refresh selected", RefreshSelectedCommand),
            };

            ReloadList();
        }

        private void EnqueueScan(ICollection<ITrackerProductViewModel> products)
        {
            _scanContext.NotifyStarted(products.Count);
            foreach (var product in products)
            {
                product.RefreshPriceCommand.Execute(null);
            }
        }

        private void ReloadList()
        {
            IsAddEditProductVisible = false;
            var productVms = _productQuery.GetAll()
                .Select(x => _vmFactory.CreateTrackerProduct(x))
                .ToList();
            foreach (var productVm in productVms)
            {
                productVm.WhenChanged(x => x.Status, status =>
                    _scanContext.NotifyProgressChange(status));
            }
            Products.ReplaceItems(productVms);
        }

        public List<DropDownMenuItem> RefreshOptions { get; }

        public ObservableCollection<ITrackerProductViewModel> Products { get; }
            = new TypedObservableList<ITrackerProductViewModel, TrackerProductViewModel>();

        [FilterContext]
        public string Filter
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        public bool IsAddEditProductVisible
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        public ITrackerProductViewModel EditingProduct
        {
            get => GetOrDefault<ITrackerProductViewModel>();
            set => RaiseAndSetIfChanged(value);
        }

        public ICommand RefreshAllCommand { get; }
        public ICommand RefreshSelectedCommand { get; }
        public ICommand OpenAddProductFlyoutCommand { get; }
        public ICommand OpenEditProductFlyoutCommand { get; }
        public ICommand DeleteProductCommand { get; }
    }
}
