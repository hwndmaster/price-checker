namespace Genius.PriceChecker.Core.Models;

public sealed record Product : EntityBase<Guid, ProductRef>
{
    public string? Category { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ProductSource[] Sources { get; set; } = [];
    public ProductPrice? Lowest { get; set; }
    public ProductPrice[] Recent { get; set; } = [];
}
