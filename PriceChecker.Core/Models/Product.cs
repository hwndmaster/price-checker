using Genius.Atom.Infrastructure.Entities;

namespace Genius.PriceChecker.Core.Models;

public class Product : EntityBase
{
    public string? Category { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ProductSource[] Sources { get; set; } = Array.Empty<ProductSource>();
    public ProductPrice? Lowest { get; set; }
    public ProductPrice[] Recent { get; set; } = Array.Empty<ProductPrice>();
}
