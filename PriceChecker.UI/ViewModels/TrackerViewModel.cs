using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
  public class TrackerViewModel : TabViewModelBase
    {
        private readonly IEventBus _eventBus;
        private readonly IProductRepository _productRepo;
        private readonly IViewModelFactory _vmFactory;
        private readonly ITrackerScanContext _scanContext;

        public TrackerViewModel(IEventBus eventBus,
            IProductRepository productRepo,
            IViewModelFactory vmFactory,
            IUserInteraction ui,
            ITrackerScanContext scanContext)
        {
            _eventBus = eventBus;
            _productRepo = productRepo;
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
            OpenAddProductFlyoutCommand = new ActionCommand(o => {
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
            OpenEditProductFlyoutCommand = new ActionCommand(o => {
                EditingProduct = Products.FirstOrDefault(x => x.IsSelected);
                EditingProduct.CommitProductCommand.Executed
                    .Take(1)
                    .Subscribe(_ => {
                        IsAddEditProductVisible = false;
                    });
                IsAddEditProductVisible = EditingProduct != null;
            });

            DeleteProductCommand = new ActionCommand(_ => {
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
                _productRepo.Delete(selectedProduct.Id);
            });

            _eventBus.WhenFired<ProductScannedEvent>()
                .Subscribe(ev => {
                    Products.First(x => x.Id == ev.Product.Id).Reconcile(ev.LowestPriceUpdated);
                });
            _eventBus.WhenFired<ProductScanFailedEvent>()
                .Subscribe(ev => {
                    Products.First(x => x.Id == ev.Product.Id).SetFailed(ev.ErrorMessage);
                });

            Deactivated.Executed.Subscribe(_ =>
                IsAddEditProductVisible = false);

            RefreshOptions = new List<DropDownMenuItem> {
                new DropDownMenuItem("Refresh all", RefreshAllCommand),
                new DropDownMenuItem("Refresh selected", RefreshSelectedCommand),
            };

            ReloadList();

            PropertiesAreInitialized = true;
        }

        private void EnqueueScan(ICollection<TrackerProductViewModel> products)
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
            var productVms = _productRepo.GetAll()
                .Select(x => _vmFactory.CreateTrackerProduct(x))
                .ToList();
            foreach (var productVm in productVms)
            {
                productVm.WhenChanged(x => x.Status, status => {
                    _scanContext.NotifyProgressChange(status);
                });
            }
            Products.ReplaceItems(productVms);
        }

        public static List<DropDownMenuItem> RefreshOptions { get; private set; }

        public ObservableCollection<TrackerProductViewModel> Products { get; } = new ObservableCollection<TrackerProductViewModel>();

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

        public TrackerProductViewModel EditingProduct
        {
            get => GetOrDefault<TrackerProductViewModel>();
            set => RaiseAndSetIfChanged(value);
        }

        public ICommand RefreshAllCommand { get; }
        public ICommand RefreshSelectedCommand { get; }
        public ICommand OpenAddProductFlyoutCommand { get; }
        public ICommand OpenEditProductFlyoutCommand { get; }
        public ICommand DeleteProductCommand { get; }
    }
}
