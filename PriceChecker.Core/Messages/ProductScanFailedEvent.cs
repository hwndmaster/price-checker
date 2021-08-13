using Genius.PriceChecker.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductScanFailedEvent : IEventMessage
    {
        public ProductScanFailedEvent(Product product, string errorMessage)
        {
            Product = product;
            ErrorMessage = errorMessage;
        }

        public Product Product { get; }
        public string ErrorMessage { get; }
    }
}