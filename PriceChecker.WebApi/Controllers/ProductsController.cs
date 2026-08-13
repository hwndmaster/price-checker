using Genius.Atom.Data.Validation;
using Genius.Atom.Web.Controllers;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;
using Microsoft.AspNetCore.Mvc;

namespace Genius.PriceChecker.WebApi.Controllers;

public sealed class ProductsController : BaseCrudController<Guid, ProductRef, ProductDto, IProductsRepository, CreateProductRequest, UpdateProductRequest>
{
    public ProductsController(IProductsRepository productsRepository, IRequestValidators requestValidators)
        : base(productsRepository, requestValidators)
    {
    }

    [HttpGet("overview")]
    public async Task<IEnumerable<ProductOverviewDto>> GetOverview(CancellationToken cancellationToken)
        => await Repository.GetOverviewAsync(cancellationToken).ConfigureAwait(false);

    [HttpGet("{productId}/prices")]
    public async Task<IEnumerable<ProductPriceDto>> GetPrices([FromRoute] Guid productId, CancellationToken cancellationToken)
        => await Repository.GetPricesAsync(productId, cancellationToken).ConfigureAwait(false);

    [HttpPost("{productId}/drop-prices")]
    public async Task<IActionResult> DropPrices([FromRoute] Guid productId, CancellationToken cancellationToken)
    {
        await Repository.DropPricesAsync(productId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
