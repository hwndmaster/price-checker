using System.Reactive.Concurrency;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.Atom.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface IProductPriceManager : IDisposable
{
    Task EnqueueScanAsync(Guid productId);
    void AutoRefreshInitialize();
}

internal sealed class ProductPriceManager : IProductPriceManager
{
    private readonly IDateTime _dateTime;
    private readonly IProductRepository _productRepo;
    private readonly IProductQueryService _productQuery;
    private readonly IPriceSeeker _priceSeeker;
    private readonly IEventBus _eventBus;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ILogger<ProductPriceManager> _logger;

    private IDisposable? _scheduledAutoRefresh;
    private int _previousAutoRefreshMinutes;

    private readonly TimeSpan _recentPeriod = TimeSpan.FromHours(3);

    public ProductPriceManager(
        IDateTime dateTime,
        IProductRepository productRepo,
        IProductQueryService productQuery,
        IPriceSeeker priceSeeker, IEventBus eventBus,
        ISettingsRepository settingsRepo,
        ILogger<ProductPriceManager> logger)
    {
        _dateTime = dateTime.NotNull();
        _productRepo = productRepo.NotNull();
        _productQuery = productQuery.NotNull();
        _priceSeeker = priceSeeker.NotNull();
        _eventBus = eventBus.NotNull();
        _settingsRepo = settingsRepo.NotNull();
        _logger = logger.NotNull();

        eventBus.WhenFired<SettingsUpdatedEvent>().Subscribe(args => {
            if (args.Settings.AutoRefreshEnabled && _scheduledAutoRefresh is null
                || args.Settings.AutoRefreshMinutes != _previousAutoRefreshMinutes)
            {
                _scheduledAutoRefresh?.Dispose();
                AutoRefreshInitialize();
            }
        });
    }

    public async Task EnqueueScanAsync(Guid productId)
    {
        var product = await _productQuery.FindByIdAsync(productId);
        if (product == null)
        {
            _logger.LogError("Product with ID '{ProductId}' was not found.", productId);
            return;
        }

        await EnqueueScanAsync(product, ignoreRecentDate: true, CancellationToken.None);
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
                    var products = (await _productQuery.GetAllAsync()).ToList();
                    _eventBus.Publish(new ProductAutoScanStartedEvent(products.Count));
                    foreach (var product in products)
                        tasks.Add(EnqueueScanAsync(product, ignoreRecentDate: false, cancel));
                }

                await Task.WhenAll(tasks);

                AutoRefreshInitialize();
            });
    }

    private Task EnqueueScanAsync(Product product, bool ignoreRecentDate, CancellationToken cancel)
    {
        return Task.Run(async() =>
        {
            try
            {
                await ScanForPricesAsync(product, ignoreRecentDate, cancel);
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new ProductScanFailedEvent(product.Id, ex.Message));
                throw;
            }
        }, cancel);
    }

    private async Task ScanForPricesAsync(Product product, bool ignoreRecentDate, CancellationToken cancel)
    {
        if (!ignoreRecentDate && IsTooRecent(product))
        {
            _logger.LogTrace("Price scanning '{ProductName}' cancelled due to recent results", product.Name);
            _eventBus.Publish(new ProductScannedEvent(product.Id, ProductScanStatus.ScannedOk));
            return;
        }

        _eventBus.Publish(new ProductScanStartedEvent(product.Id));

        _logger.LogTrace("Processing '{ProductName}'", product.Name);
        var results = await _priceSeeker.SeekAsync(product, cancel);
        if (!results.Any())
        {
            _logger.LogWarning("Price scanning for '{ProductName}' failed or no results retrieved", product.Name);
            _eventBus.Publish(new ProductScanFailedEvent(product.Id, "Scan failed or no results retrieved"));
            return;
        }

        product.Recent = LogAndConvert(product, results);

        var lowest = product.MinPrice();
        var status = ProductScanStatus.ScannedOk;
        if (!product.Recent.Any())
        {
            status = ProductScanStatus.Failed;
        }
        else if (product.Lowest is not null
            && lowest is not null
            && product.Lowest.Price > lowest.Price)
        {
            status = ProductScanStatus.ScannedNewLowest;
        }
        else if (product.Recent.Any(x => x.Status != AgentHandlingStatus.Success))
        {
            status = ProductScanStatus.ScannedWithErrors;
        }
        product.Lowest = lowest;

        await _productRepo.StoreAsync(product);

        _eventBus.Publish(new ProductScannedEvent(product.Id, status));
    }

    private bool IsTooRecent(Product product)
    {
        if (product.Recent.Length == 0)
            return false;

        var recentDate = product.Recent.Max(x => x.FoundDate);
        return _dateTime.NowUtc - recentDate < _recentPeriod;
    }

    private ProductPrice[] LogAndConvert(Product product, IEnumerable<PriceSeekResult> results)
    {
        var converted = results.Select(x => new ProductPrice
        {
            ProductSourceId = x.ProductSourceId,
            Status = x.Status,
            Price = x.Price,
            FoundDate = _dateTime.NowUtc
        }).ToArray();

        _logger.LogTrace("Results retrieved for '{ProductName}': {Results}",
            product.Name,
            string.Join(", ", converted.ToList()));

        return converted;
    }
}
