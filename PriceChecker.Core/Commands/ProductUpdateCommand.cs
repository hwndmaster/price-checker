using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands.UpdatableData;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Commands;

public sealed class ProductUpdateCommand : ProductUpdatableData, ICommandMessage
{
    public ProductUpdateCommand(Guid productId, string name, string? category, string? description, ProductSource[] sources)
        : base(name, category, description, sources)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
