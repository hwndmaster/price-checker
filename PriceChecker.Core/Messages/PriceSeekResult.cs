using System;

namespace Genius.PriceChecker.Core.Messages
{
    public sealed class PriceSeekResult
    {
        public Guid ProductSourceId { get; set; }
        public string ProductId { get; set; }
        public string AgentId { get; set; }
        public decimal Price { get; set; }
    }
}
