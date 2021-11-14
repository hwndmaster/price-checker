using System.Threading.Tasks;
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

    public Task ProcessAsync(ProductDropPricesCommand command)
    {
        var product = _productQuery.FindById(command.ProductId);
        Guard.AgainstNull(product, nameof(product));

        product.Lowest = null;
        product.Recent = Array.Empty<ProductPrice>();

        _productRepo.Store(product);

        return Task.CompletedTask;
    }
}
