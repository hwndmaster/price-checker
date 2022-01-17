using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class ProductDropPricesCommandHandler : ICommandHandler<ProductDropPricesCommand>
{
    private readonly IProductRepository _productRepo;
    private readonly IProductQueryService _productQuery;

    public ProductDropPricesCommandHandler(IProductRepository productRepo, IProductQueryService productQuery)
    {
        _productRepo = productRepo;
        _productQuery = productQuery;
    }

    public async Task ProcessAsync(ProductDropPricesCommand command)
    {
        var product = await _productQuery.FindByIdAsync(command.ProductId);
        Guard.NotNull(product);

        product.Lowest = null;
        product.Recent = Array.Empty<ProductPrice>();

        await _productRepo.StoreAsync(product);
    }
}
