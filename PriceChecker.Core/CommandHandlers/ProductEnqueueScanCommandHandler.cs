using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Tasks;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class ProductEnqueueScanCommandHandler : ICommandHandler<ProductEnqueueScanCommand>
{
    private readonly IProductPriceManager _productMng;

    public ProductEnqueueScanCommandHandler(IProductPriceManager productMng)
    {
        _productMng = productMng;
    }

    public Task ProcessAsync(ProductEnqueueScanCommand command)
    {
        _productMng.EnqueueScanAsync(command.ProductId).RunAndForget();

        return Task.CompletedTask;
    }
}
