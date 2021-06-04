using System;

namespace Genius.PriceChecker.Core.Models
{
    public class ProductSource
    {
        public Guid Id { get; set; }
        public string AgentId { get; set; }
        public string AgentArgument { get; set; }
    }
}