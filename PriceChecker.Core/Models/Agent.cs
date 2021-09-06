using Genius.Atom.Infrastructure.Entities;

namespace Genius.PriceChecker.Core.Models
{
    public class Agent : EntityBase
    {
        public string Key { get; set; }
        public string Url { get; set; }
        public string PricePattern { get; set; }
        public char DecimalDelimiter { get; set; }
    }
}
