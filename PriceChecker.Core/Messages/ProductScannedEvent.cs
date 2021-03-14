using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductScannedEvent : IEventMessage
    {
        public ProductScannedEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; }
    }
}