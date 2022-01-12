using System.Threading.Tasks;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Commands.UpdatableData;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class ProductCreateOrUpdateCommandHandler:
    ICommandHandler<ProductCreateCommand, Guid>,
    ICommandHandler<ProductUpdateCommand>
{
    private readonly IProductRepository _productRepo;
    private readonly IProductQueryService _productQuery;
    private readonly IEventBus _eventBus;

    public ProductCreateOrUpdateCommandHandler(IProductRepository productRepo, IProductQueryService productQuery, IEventBus eventBus)
    {
        _productRepo = productRepo;
        _productQuery = productQuery;
        _eventBus = eventBus;
    }

    public Task<Guid> ProcessAsync(ProductCreateCommand command)
    {
        var product = new Product();
        UpdateProperties(product, command);
        _productRepo.Store(product);

        _eventBus.Publish(new ProductsAffectedEvent());

        return Task.FromResult(product.Id);
    }

    public async Task ProcessAsync(ProductUpdateCommand command)
    {
        var product = await _productQuery.FindByIdAsync(command.ProductId);
        Guard.NotNull(product);

        UpdateProperties(product, command);
        _productRepo.Store(product);

        _eventBus.Publish(new ProductsAffectedEvent());
    }

    private static void UpdateProperties(Product product, ProductUpdatableData command)
    {
        product.Name = command.Name;
        product.Category = command.Category;
        product.Description = command.Description;
        product.Sources = command.Sources;
    }
}
