using System;
using System.Diagnostics.CodeAnalysis;

namespace Genius.PriceChecker.Core.Models
{
    public class Product
    {
        public Guid Id { get; set; }

        [MaybeNull]
        public string Category { get; set; }
        [NotNull]
        public string Name { get; set; }
        [MaybeNull]
        public string Description { get; set; }
        [NotNull]
        public ProductSource[] Sources { get; set; } = new ProductSource[0];
        [MaybeNull]
        public ProductPrice Lowest { get; set; }
        [NotNull]
        public ProductPrice[] Recent { get; set; } = new ProductPrice[0];
    }
}