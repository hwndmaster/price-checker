using Genius.PriceChecker.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductAutoScanStartedEvent : IEventMessage
    {
        public ProductAutoScanStartedEvent(int productsCount)
        {
            ProductsCount = productsCount;
        }

        public int ProductsCount { get; }
    }
}
