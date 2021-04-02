using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services
{
    public interface IProductPriceManager : IDisposable
    {
        void EnqueueScan(Guid productId);
    }

    internal sealed class ProductPriceManager : IProductPriceManager
    {
        private readonly IProductRepository _productRepo;
        private readonly IPriceSeeker _priceSeeker;
        private readonly IEventBus _eventBus;
        private readonly ILogger<ProductPriceManager> _logger;

        private readonly ProductPriceTaskScheduler _taskScheduler;

        private readonly TimeSpan RecentPeriod = TimeSpan.FromHours(3);

        public ProductPriceManager(IProductRepository productRepo,
            IPriceSeeker priceSeeker, IEventBus eventBus,
            ILogger<ProductPriceManager> logger)
        {
            _productRepo = productRepo;
            _priceSeeker = priceSeeker;
            _eventBus = eventBus;
            _logger = logger;

            _taskScheduler = new ProductPriceTaskScheduler();
        }

        public void EnqueueScan(Guid productId)
        {
            var product = _productRepo.FindById(productId);
            if (product == null)
            {
                _logger.LogError($"Product with ID '{productId}' was not found.");
                return;
            }

            Task.Factory.StartNew(async() =>
            {
                try
                {
                    await ScanForPricesAsync(product, true);
                }
                catch (Exception ex)
                {
                    _eventBus.Publish(new ProductScanFailedEvent(product, ex.Message));
                    throw;
                }
            }, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        private async Task ScanForPricesAsync(Product product, bool ignoreRecentDate = false)
        {
            if (IsTooRecent(product))
            {
                _logger.LogTrace($"Price scanning '{product.Name}' cancelled due to recent results");
                //_eventBus.Publish(new ProductScanFailedEvent(product, "Scan cancelled due to recent results"));
                _eventBus.Publish(new ProductScannedEvent(product, false));
                return;
            }

            _logger.LogTrace($"Processing '{product.Name}'");
            var results = await _priceSeeker.SeekAsync(product);
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
                    //_logger.LogWarning($"Price for '{product.Name}' has dropped lower than before! New price is {minPrice} EUR on '{product.Lowest.AgentId}' (was {product.Lowest.Price} EUR)");
                }
                product.Lowest = product.Recent.First(x => x.Price == minPrice);

                _productRepo.Store(product);

                _eventBus.Publish(new ProductScannedEvent(product, lowestPriceUpdated));
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
            var converted = results.Select(x => new ProductPrice()
            {
                AgentId = x.AgentId,
                    Price = x.Price,
                    FoundDate = DateTime.Now
            }).ToArray();

            _logger.LogTrace($"Results retrieved for '{product.Name}': {string.Join(", ", converted.ToList())}");

            return converted;
        }

        public void Dispose()
        {
            _taskScheduler.Dispose();
        }
    }
}
