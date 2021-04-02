using System;
using System.Linq;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Services
{
    public interface IProductStatusProvider
    {
        ProductScanStatus DetermineStatus(Product product);
    }

    internal sealed class ProductStatusProvider : IProductStatusProvider
    {
        private readonly TimeSpan OutdatedPeriod = TimeSpan.FromHours(20);

        public ProductScanStatus DetermineStatus(Product product)
        {
            if (product.Recent.Length == 0)
                return ProductScanStatus.NotScanned;

            if (DateTime.Now - product.Recent.Max(x => x.FoundDate) > OutdatedPeriod)
            {
                return ProductScanStatus.Outdated;
            }

            return ProductScanStatus.ScannedOk;
        }
    }
}
