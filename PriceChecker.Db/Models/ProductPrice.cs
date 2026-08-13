using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Db.Models;

public sealed record ProductPrice : EntityBase<Guid, ProductPriceRef>
{
    public required ProductSourceRef ProductSourceId { get; init; }
    public required AgentHandlingStatus Status { get; init; }
    public decimal? Price { get; init; }
    public required DateTimeOffset FoundDate { get; init; }

    // Relations:
    public ProductSource ProductSource { get; init; } = null!;

    /// <summary>
    ///   Creates a <see cref="ProductPrice"/> instance without linking the related entities.
    /// </summary>
    public static ProductPrice CreateWithNoLinking(Guid productSourceId, AgentHandlingStatus status,
        decimal? price, DateTimeOffset foundDate, DateTimeOffset? date = null, Guid? id = null)
        => new()
        {
            ProductSourceId = productSourceId,
            Status = status,
            Price = price,
            FoundDate = foundDate,
            DateCreated = date ?? DateTimeOffset.MinValue,
            LastModified = date ?? DateTimeOffset.MinValue,
            Id = id ?? Guid.NewGuid(),
        };
}
