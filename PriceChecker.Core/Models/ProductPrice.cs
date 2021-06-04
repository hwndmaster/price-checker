using System;

namespace Genius.PriceChecker.Core.Models
{
    public class ProductPrice
    {
        public Guid ProductSourceId { get; set; }
        public decimal Price { get; set; }
        public DateTime FoundDate { get; set; }

        public override string ToString()
        {
            return $"{ProductSourceId} = {Price}";
        }
    }
}
