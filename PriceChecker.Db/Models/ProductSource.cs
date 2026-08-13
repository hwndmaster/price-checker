using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Db.Models;

public sealed record ProductSource : EntityBase<Guid, ProductSourceRef>
{
    public ProductSource()
    {
        Prices = [];
    }

    public required ProductRef ProductId { get; init; }
    public required AgentRef AgentId { get; init; }
    public required string AgentArgument { get; init; }

    // Relations:
    public Product Product { get; init; } = null!;
    public Agent Agent { get; init; } = null!;
    public ICollection<ProductPrice> Prices { get; init; }

    /// <summary>
    ///   Creates a <see cref="ProductSource"/> instance without linking the related entities.
    /// </summary>
    public static ProductSource CreateWithNoLinking(Guid productId, Guid agentId, string agentArgument,
        DateTimeOffset? date = null, Guid? id = null)
        => new()
        {
            ProductId = productId,
            AgentId = agentId,
            AgentArgument = agentArgument,
            DateCreated = date ?? DateTimeOffset.MinValue,
            LastModified = date ?? DateTimeOffset.MinValue,
            Id = id ?? Guid.NewGuid(),
        };
}
