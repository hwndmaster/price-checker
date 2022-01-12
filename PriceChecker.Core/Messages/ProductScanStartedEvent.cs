using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages;

public sealed class ProductScanStartedEvent : IEventMessage
{
    public ProductScanStartedEvent(Guid productId)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
