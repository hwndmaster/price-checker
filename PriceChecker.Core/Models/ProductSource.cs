using System.Text.Json.Serialization;

namespace Genius.PriceChecker.Core.Models;

public class ProductSource
{
    public Guid Id { get; set; }
    public string AgentKey { get; set; } = null!;
    public string AgentArgument { get; set; } = null!;

    // Relations:
    [JsonIgnore]
    public Product Product { get; internal set; } = null!;
    [JsonIgnore]
    public Agent Agent { get; internal set; } = null!;
}
