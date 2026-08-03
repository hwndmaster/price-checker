namespace Genius.PriceChecker.Core.Models;

public sealed record Agent : EntityBase<Guid, AgentRef>
{
    public string Key { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PricePattern { get; set; } = null!;
    public string Handler { get; set; } = null!;
    public char DecimalDelimiter { get; set; }
}
