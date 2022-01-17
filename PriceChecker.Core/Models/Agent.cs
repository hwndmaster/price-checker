using Genius.Atom.Infrastructure.Entities;

namespace Genius.PriceChecker.Core.Models;

public class Agent : EntityBase, ICloneable
{
    public string Key { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string PricePattern { get; set; } = null!;
    public string Handler { get; set; } = null!;
    public char DecimalDelimiter { get; set; }

    public object Clone()
    {
        return new Agent()
        {
            Id = Id,
            Key = Key,
            Url = Url,
            PricePattern = PricePattern,
            Handler = Handler,
            DecimalDelimiter = DecimalDelimiter
        };
    }
}
