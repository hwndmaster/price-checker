namespace Genius.PriceChecker.Core.Models
{
    public class Agent
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string PricePattern { get; set; }
        public char DecimalDelimiter { get; set; }
    }
}
