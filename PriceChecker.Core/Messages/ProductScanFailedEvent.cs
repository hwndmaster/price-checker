using Genius.Atom.Infrastructure.Events;

namespace Genius.PriceChecker.Core.Messages;

public sealed class ProductScanFailedEvent : IEventMessage
{
    public ProductScanFailedEvent(Guid productId, string errorMessage)
    {
        ProductId = productId;
        ErrorMessage = errorMessage;
    }

    public Guid ProductId { get; }
    public string ErrorMessage { get; }
}
