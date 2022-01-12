using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IProductQueryService : IQueryService<Product>
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

    public Task<Product?> FindByIdAsync(Guid entityId)
    {
        return Task.FromResult(base.FindById(entityId));
    }

    public Task<IEnumerable<Product>> GetAllAsync()
    {
        return Task.FromResult(base.GetAll());
    }

    protected override void FillUpRelations(Product product)
    {
        var sourcesDict = product.Sources.ToDictionary(x => x.Id);

        foreach (var productSource in product.Sources)
        {
            productSource.Product = product;
            productSource.Agent = _agentRepo.FindByKey(productSource.AgentKey).NotNull();
        }
        foreach (var productPrice in product.Recent)
        {
            productPrice.ProductSource = sourcesDict[productPrice.ProductSourceId];
        }
    }
}
