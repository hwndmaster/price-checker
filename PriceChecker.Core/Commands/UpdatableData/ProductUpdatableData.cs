using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Commands.UpdatableData
{
    public abstract class ProductUpdatableData
    {
        protected ProductUpdatableData(string name, string category, string description, ProductSource[] sources)
        {
            Name = name;
            Category = category;
            Description = description;
            Sources = sources;
        }

        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public ProductSource[] Sources { get; }
    }
}
