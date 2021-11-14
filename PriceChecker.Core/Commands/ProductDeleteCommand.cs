using Genius.Atom.Infrastructure.Commands;

namespace Genius.PriceChecker.Core.Commands;

public sealed class ProductDeleteCommand : ICommandMessage
{
    public ProductDeleteCommand(Guid productId)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
