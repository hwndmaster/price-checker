using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Db.Models;

public sealed record Product : EntityBase<Guid, ProductRef>
{
    public Product()
    {
        Sources = [];
    }

    public required string Name { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }

    // Relations:
    public ICollection<ProductSource> Sources { get; init; }

    /// <summary>
    ///   Creates a <see cref="Product"/> instance without linking the related entities.
    /// </summary>
    public static Product CreateWithNoLinking(string name, string? category = null, string? description = null,
        DateTimeOffset? date = null, Guid? id = null)
        => new()
        {
            Name = name,
            Category = category,
            Description = description,
            DateCreated = date ?? DateTimeOffset.MinValue,
            LastModified = date ?? DateTimeOffset.MinValue,
            Id = id ?? Guid.NewGuid(),
        };
}
