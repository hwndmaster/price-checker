using Genius.Atom.Infrastructure.Commands;

namespace Genius.PriceChecker.Core.Commands;

public sealed class ProductDropPricesCommand : ICommandMessage
{
    public ProductDropPricesCommand(Guid productId)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
