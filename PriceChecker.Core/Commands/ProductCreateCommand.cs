using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands.UpdatableData;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Commands;

public sealed class ProductCreateCommand : ProductUpdatableData, ICommandMessageExchange<Guid>
{
    public ProductCreateCommand(string name, string? category, string? description, ProductSource[] sources)
        : base(name, category, description, sources)
    {
    }
}
