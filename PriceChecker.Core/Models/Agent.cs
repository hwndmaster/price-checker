using Genius.Atom.Infrastructure.Entities;

namespace Genius.PriceChecker.Core.Models;

public class Agent : EntityBase
{
    public string Key { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PricePattern { get; set; } = null!;
    public char DecimalDelimiter { get; set; }
}
