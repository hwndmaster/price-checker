using System.Text.Json.Serialization;

namespace Genius.PriceChecker.Core.Models;

public class ProductPrice
{
    public Guid ProductSourceId { get; set; }
    [JsonIgnore]
    public ProductSource ProductSource { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime FoundDate { get; set; }

    public override string ToString()
    {
        return $"{ProductSource?.Product?.Name ?? ProductSourceId.ToString()} = {Price}";
    }
}
