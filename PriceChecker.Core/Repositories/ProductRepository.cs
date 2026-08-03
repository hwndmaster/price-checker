using Genius.Atom.Data.IdHandlers;
using Genius.Atom.Data.JsonPersistence;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IProductQueryService : IQueryService<Product>
{
}

public interface IProductRepository : IJsonRepository<Guid, ProductRef, Product>
{
}

internal sealed class ProductRepository : JsonRepositoryBase<Guid, ProductRef, Product>, IProductRepository, IProductQueryService
{
    private readonly IAgentQueryService _agentRepo;

    public ProductRepository(IEventBus eventBus, IJsonPersister persister, IIdHandler<Guid> idHandler,
        IAgentQueryService agentQuery,
        ILogger<ProductRepository> logger)
        : base(eventBus, persister, idHandler, logger)
    {
        _agentRepo = agentQuery.NotNull();
    }

    public Task<Product?> FindByIdAsync(Guid entityId)
        => base.FindByIdAsync(ProductRef.Create(entityId));

    protected override async Task FillUpRelationsAsync(Product product)
    {
        var sourcesDict = product.Sources.ToDictionary(x => x.Id);

        foreach (var productSource in product.Sources)
        {
            productSource.Product = product;
            productSource.Agent = (await _agentRepo.FindByKeyAsync(productSource.AgentKey)).NotNull();
        }
        foreach (var productPrice in product.Recent)
        {
            productPrice.ProductSource = sourcesDict[productPrice.ProductSourceId];
        }
    }
}
