using System;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class PriceSeekResult
    {
        public Guid ProductSourceId { get; set; }
        public string AgentKey { get; set; }
        public decimal Price { get; set; }
    }
}
