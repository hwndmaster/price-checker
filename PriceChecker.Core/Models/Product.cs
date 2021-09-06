using System;
using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Entities;

namespace Genius.PriceChecker.Core.Models
{
    public class Product : EntityBase
    {
        [MaybeNull]
        public string Category { get; set; }
        [NotNull]
        public string Name { get; set; }
        [MaybeNull]
        public string Description { get; set; }
        [NotNull]
        public ProductSource[] Sources { get; set; } = Array.Empty<ProductSource>();
        [MaybeNull]
        public ProductPrice Lowest { get; set; }
        [NotNull]
        public ProductPrice[] Recent { get; set; } = Array.Empty<ProductPrice>();
    }
}