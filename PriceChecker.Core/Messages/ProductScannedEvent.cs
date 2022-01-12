using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Messages;

public sealed class ProductScannedEvent : IEventMessage
{
    public ProductScannedEvent(Guid productId, ProductScanStatus status)
    {
        ProductId = productId;
        Status = status;
    }

    public Guid ProductId { get; }
    public ProductScanStatus Status { get; }
}
