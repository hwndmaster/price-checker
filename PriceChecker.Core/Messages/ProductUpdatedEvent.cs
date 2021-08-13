using Genius.PriceChecker.Core.Models;
using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class ProductUpdatedEvent : IEventMessage
    {
        public ProductUpdatedEvent(Product product)
        {
            Product = product;
        }

        public Product Product { get; }
    }
}
