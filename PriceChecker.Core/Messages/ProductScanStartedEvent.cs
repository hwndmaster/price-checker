using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductScanStartedEvent : IEventMessage
    {
        public ProductScanStartedEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; }
    }
}
