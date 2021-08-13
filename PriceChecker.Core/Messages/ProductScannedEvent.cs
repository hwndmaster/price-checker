using Genius.PriceChecker.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductScannedEvent : IEventMessage
    {
        public ProductScannedEvent(Product product, bool lowestPriceUpdated)
        {
            Product = product;
            LowestPriceUpdated = lowestPriceUpdated;
        }

        public Product Product { get; }
        public bool LowestPriceUpdated { get; }
    }
}
