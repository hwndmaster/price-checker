using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class TrackerViewModel : TabViewModelBase<TrackerViewModel>
    {
        private readonly IEventBus _eventBus;
        private readonly IProductRepository _productRepo;
        private readonly IProductPriceManager _productMng;
        private readonly IViewModelFactory _vmFactory;

        public TrackerViewModel(IEventBus eventBus,
            IProductRepository productRepo,
            IProductPriceManager productMng,
            IViewModelFactory vmFactory,
            IUserInteraction ui)
        {
            _eventBus = eventBus;
            _productRepo = productRepo;
            _productMng = productMng;
            _vmFactory = vmFactory;

            RefreshAllCommand = new ActionCommand(_ => {
                IsAddEditProductVisible = false;
                EnqueueScan(Products);
            });
            RefreshSelectedCommand = new ActionCommand(_ => {
                IsAddEditProductVisible = false;
                EnqueueScan(Products.Where(x => x.IsSelected));
            });
            OpenAddProductFlyoutCommand = new ActionCommand(o => {
                IsAddEditProductVisible = !IsAddEditProductVisible;
                if (IsAddEditProductVisible)
                {
                    EditingProduct = vmFactory.CreateTrackerProduct(null);
                    EditingProduct.CommitProductCommand.Executed += (_, __) => {
                        IsAddEditProductVisible = false;
                        ReloadList();
                    };
                }
            });
            OpenEditProductFlyoutCommand = new ActionCommand(o => {
                EditingProduct = Products.FirstOrDefault(x => x.IsSelected);
                EditingProduct.CommitProductCommand.Executed += (_, __) => {
                    IsAddEditProductVisible = false;
                };
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

            _eventBus.Subscribe<ProductScannedEvent>((@event) => {
                Products.First(x => x.Id == @event.Product.Id).Reconcile(@event.LowestPriceUpdated);
            });
            _eventBus.Subscribe<ProductScanFailedEvent>((@event) => {
                Products.First(x => x.Id == @event.Product.Id).SetFailed(@event.ErrorMessage);
            });

            Deactivated.Executed += (_, __) => {
                IsAddEditProductVisible = false;
            };

            RefreshOptions = new List<DropDownMenuItem> {
                new DropDownMenuItem("Refresh all", RefreshAllCommand),
                new DropDownMenuItem("Refresh selected", RefreshSelectedCommand),
            };

            ReloadList();

            PropertiesAreInitialized = true;
        }

        private void EnqueueScan(IEnumerable<TrackerProductViewModel> products)
        {
            foreach (var product in products)
            {
                if (product.Status == ProductScanStatus.Scanning)
                    continue;
                _productMng.EnqueueScan(product.Id);
                product.Status = ProductScanStatus.Scanning;
            }
        }

        private void ReloadList()
        {
            IsAddEditProductVisible = false;
            var productVms = _productRepo.GetAll().Select(x => _vmFactory.CreateTrackerProduct(x));
            Products.ReplaceItems(productVms);
        }

        public static List<DropDownMenuItem> RefreshOptions { get; private set; }

        public ObservableCollection<TrackerProductViewModel> Products { get; } = new ObservableCollection<TrackerProductViewModel>();

        public ICommand RefreshAllCommand { get; }
        public ICommand RefreshSelectedCommand { get; }
        public ICommand OpenAddProductFlyoutCommand { get; }
        public ICommand OpenEditProductFlyoutCommand { get; }
        public ICommand DeleteProductCommand { get; }

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
    }
}
