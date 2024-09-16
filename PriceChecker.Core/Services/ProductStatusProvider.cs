using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Services;

public interface IProductStatusProvider
{
    ProductScanStatus DetermineStatus(Product product);
}

internal sealed class ProductStatusProvider : IProductStatusProvider
{
    private readonly IDateTime _dateTime;
    private readonly TimeSpan _outdatedPeriod = TimeSpan.FromHours(20);

    public ProductStatusProvider(IDateTime dateTime)
    {
        _dateTime = dateTime.NotNull();
    }

    public ProductScanStatus DetermineStatus(Product product)
    {
        if (product.Recent.Length == 0)
            return ProductScanStatus.NotScanned;

        if (_dateTime.NowUtc - product.Recent.Max(x => x.FoundDate) > _outdatedPeriod)
            return ProductScanStatus.Outdated;

        if (product.Recent.Any(x => x.Status != AgentHandlingStatus.Success))
            return ProductScanStatus.ScannedWithErrors;

        return ProductScanStatus.ScannedOk;
    }
}
