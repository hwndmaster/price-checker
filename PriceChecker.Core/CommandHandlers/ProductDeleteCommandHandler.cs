using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class ProductDeleteCommandHandler : ICommandHandler<ProductDeleteCommand>
{
    private readonly IProductRepository _productRepo;
    private readonly IEventBus _eventBus;

    public ProductDeleteCommandHandler(IProductRepository productRepo, IEventBus eventBus)
    {
        _productRepo = productRepo;
        _eventBus = eventBus;
    }

    public async Task ProcessAsync(ProductDeleteCommand command)
    {
        await _productRepo.DeleteAsync(command.ProductId);

        _eventBus.Publish(new ProductsAffectedEvent());
    }
}
