using System;
using System.Text.Json.Serialization;

namespace Genius.PriceChecker.Core.Models
{
    public class ProductSource
    {
        public Guid Id { get; set; }
        public string AgentKey { get; set; }
        public string AgentArgument { get; set; }

        // Relations:
        [JsonIgnore]
        public Product Product { get; internal set; }
        [JsonIgnore]
        public Agent Agent { get; internal set; }
    }
}