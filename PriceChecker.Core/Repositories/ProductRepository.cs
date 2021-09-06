using System;
using System.Linq;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Persistence;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories
{
    public interface IProductQueryService : IEntityQueryService<Product>
    {
    }

    public interface IProductRepository : IRepository<Product>
    {
    }

    internal sealed class ProductRepository : RepositoryBase<Product>, IProductRepository, IProductQueryService
    {
        private readonly IAgentQueryService _agentRepo;

        public ProductRepository(IEventBus eventBus, IJsonPersister persister,
            IAgentQueryService agentQuery,
            ILogger<ProductRepository> logger)
            : base(eventBus, persister, logger)
        {
            _agentRepo = agentQuery;
        }

        protected override void FillupRelations(Product product)
        {
            var sourcesDict = product.Sources.ToDictionary(x => x.Id);

            foreach (var productSource in product.Sources)
            {
                productSource.Product = product;
                productSource.Agent = _agentRepo.FindByKey(productSource.AgentKey);
            }
            foreach (var productPrice in product.Recent)
            {
                productPrice.ProductSource = sourcesDict[productPrice.ProductSourceId];
            }
        }
    }
}
