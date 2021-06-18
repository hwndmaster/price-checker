using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services
{
  public interface IProductPriceManager : IDisposable
    {
        void EnqueueScan(Guid productId);
        void AutoRefreshInitialize();
    }

    internal sealed class ProductPriceManager : IProductPriceManager
    {
        private readonly IProductRepository _productRepo;
        private readonly IPriceSeeker _priceSeeker;
        private readonly IEventBus _eventBus;
        private readonly ISettingsRepository _settingsRepo;
        private readonly ILogger<ProductPriceManager> _logger;

        private IDisposable _scheduledAutoRefresh;
        private int _previousAutoRefreshMinutes;

        private readonly TimeSpan RecentPeriod = TimeSpan.FromHours(3);

        public ProductPriceManager(IProductRepository productRepo,
            IPriceSeeker priceSeeker, IEventBus eventBus,
            ISettingsRepository settingsRepo,
            ILogger<ProductPriceManager> logger)
        {
            _productRepo = productRepo;
            _priceSeeker = priceSeeker;
            _eventBus = eventBus;
            _settingsRepo = settingsRepo;
            _logger = logger;

            eventBus.WhenFired<SettingsUpdatedEvent>().Subscribe(args => {
                if (args.Settings.AutoRefreshEnabled && _scheduledAutoRefresh == null
                    || args.Settings.AutoRefreshMinutes != _previousAutoRefreshMinutes)
                {
                    _scheduledAutoRefresh?.Dispose();
                    AutoRefreshInitialize();
                }
            });
        }

        public void EnqueueScan(Guid productId)
        {
            var product = _productRepo.FindById(productId);
            if (product == null)
            {
                _logger.LogError($"Product with ID '{productId}' was not found.");
                return;
            }

            EnqueueScan(product, ignoreRecentDate: true, CancellationToken.None);
        }

        public void Dispose()
        {
            _scheduledAutoRefresh?.Dispose();
        }

        public void AutoRefreshInitialize()
        {
            _scheduledAutoRefresh?.Dispose();
            _scheduledAutoRefresh = null;

            if (!_settingsRepo.Get().AutoRefreshEnabled)
            {
                return;
            }

            _previousAutoRefreshMinutes = _settingsRepo.Get().AutoRefreshMinutes;

            _scheduledAutoRefresh = TaskPoolScheduler.Default.ScheduleAsync(
                TimeSpan.FromMinutes(_previousAutoRefreshMinutes),
                async (scheduler, cancel) => {
                    _logger.LogInformation("AutoRefresh worker started.");
                    List<Task> tasks = new();
                    if (_settingsRepo.Get().AutoRefreshEnabled)
                    {
                        var products = _productRepo.GetAll().ToList();
                        _eventBus.Publish(new ProductAutoScanStartedEvent(products.Count));
                        foreach (var product in products)
                            tasks.Add(EnqueueScan(product, ignoreRecentDate: false, cancel));
                    }

                    await Task.WhenAll(tasks);

                    AutoRefreshInitialize();
                });
        }

        private Task EnqueueScan(Product product, bool ignoreRecentDate, CancellationToken cancel)
        {
            return Task.Run(async() =>
            {
                try
                {
                    await ScanForPricesAsync(product, ignoreRecentDate, cancel);
                }
                catch (Exception ex)
                {
                    _eventBus.Publish(new ProductScanFailedEvent(product, ex.Message));
                    throw;
                }
            }, cancel);
        }

        private async Task ScanForPricesAsync(Product product, bool ignoreRecentDate, CancellationToken cancel)
        {
            if (!ignoreRecentDate && IsTooRecent(product))
            {
                _logger.LogTrace($"Price scanning '{product.Name}' cancelled due to recent results");
                //_eventBus.Publish(new ProductScanFailedEvent(product, "Scan cancelled due to recent results"));
                _eventBus.Publish(new ProductScannedEvent(product, false));
                return;
            }

            _eventBus.Publish(new ProductScanStartedEvent(product));

            _logger.LogTrace($"Processing '{product.Name}'");
            var results = await _priceSeeker.SeekAsync(product, cancel);
            if (!results.Any())
            {
                _logger.LogWarning($"Price scanning for '{product.Name}' failed or no results retrieved");
                _eventBus.Publish(new ProductScanFailedEvent(product, "Scan failed or no results retrieved"));
                return;
            }

            product.Recent = LogAndConvert(product, results);

            var lowestPriceUpdated = false;
            var minPrice = product.Recent.Min(x => x.Price);
            if (product.Lowest == null || product.Lowest.Price >= minPrice)
            {
                if (product.Lowest != null && product.Lowest.Price > minPrice)
                {
                    lowestPriceUpdated = true;
                }
                product.Lowest = product.Recent.First(x => x.Price == minPrice);

                _productRepo.Store(product);
            }

            _eventBus.Publish(new ProductScannedEvent(product, lowestPriceUpdated));
        }

        private bool IsTooRecent(Product product)
        {
            if (product.Recent.Length == 0)
                return false;

            var recentDate = product.Recent.Max(x => x.FoundDate);
            return DateTime.Now - recentDate < RecentPeriod;
        }

        private ProductPrice[] LogAndConvert(Product product, IEnumerable<PriceSeekResult> results)
        {
            var converted = results.Select(x => new ProductPrice
            {
                ProductSourceId = x.ProductSourceId,
                Price = x.Price,
                FoundDate = DateTime.Now
            }).ToArray();

            _logger.LogTrace($"Results retrieved for '{product.Name}': {string.Join(", ", converted.ToList())}");

            return converted;
        }
    }
}
