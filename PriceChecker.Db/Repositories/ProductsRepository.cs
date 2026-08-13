using System.Linq.Expressions;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Models;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;
using Microsoft.EntityFrameworkCore;

namespace Genius.PriceChecker.Db.Repositories;

public interface IProductsRepository : IRepository<Guid, ProductRef, ProductDto, CreateProductRequest, UpdateProductRequest>
{
    Task<IEnumerable<ProductOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<ProductOverviewDto?> GetOverviewByIdAsync(ProductRef productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScanProduct>> GetScanProductsAsync(IEnumerable<ProductRef>? productIds = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductPriceDto>> GetPricesAsync(ProductRef productId, CancellationToken cancellationToken = default);
    Task AddScanResultsAsync(ProductRef productId, IReadOnlyCollection<PriceSeekResult> results, CancellationToken cancellationToken = default);
    Task DropPricesAsync(ProductRef productId, CancellationToken cancellationToken = default);
}

internal sealed class ProductsRepository
    : BaseRepository<Product, Guid, ProductRef, ProductDto, CreateProductRequest, UpdateProductRequest>, IProductsRepository
{
    private readonly IDateTime _dateTime;
    private readonly IProductStatusProvider _statusProvider;

    public ProductsRepository(IDateTime dateTime, IDatabaseContext databaseContext,
        IProductStatusProvider statusProvider)
        : base(dateTime, databaseContext)
    {
        _dateTime = dateTime.NotNull();
        _statusProvider = statusProvider.NotNull();
    }

    protected override Expression<Func<Product, ProductDto>> ProjectToGetDto { get; }
        = p => new ProductDto(p.Id, p.Name, p.Category, p.Description,
            p.Sources.Select(s => new ProductSourceDto(s.Id, s.AgentId, s.AgentArgument)).ToArray(),
            p.DateCreated, p.LastModified);

    protected override Product MapCreateDto(CreateProductRequest dto)
    {
        var product = Product.CreateWithNoLinking(dto.Name, dto.Category, dto.Description);
        foreach (var source in dto.Sources)
        {
            product.Sources.Add(ProductSource.CreateWithNoLinking(product.Id, source.AgentId, source.AgentArgument));
        }

        return product;
    }

    protected override Product MapUpdateDto(UpdateProductRequest dto, Product existingEntity)
        => existingEntity with
        {
            Name = dto.Name,
            Category = dto.Category,
            Description = dto.Description,
        };

    protected override async Task AfterUpdateAsync(UpdateProductRequest updateRequest, Product updatedEntity, CancellationToken cancellationToken)
    {
        // Synchronize the product sources: sources without an ID are created,
        // sources missing from the request are removed.
        var sourcesSet = GetContext().Set<ProductSource>();
        var existingSources = await sourcesSet
            .Where(BelongsToProduct(updatedEntity.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var requestedIds = updateRequest.Sources
            .Where(x => x.Id is not null)
            .Select(x => x.Id!)
            .ToHashSet();

        foreach (var removedSource in existingSources.Where(s => !requestedIds.Contains(s.Id)))
        {
            sourcesSet.Remove(removedSource);
        }

        foreach (var requestedSource in updateRequest.Sources)
        {
            var existingSource = requestedSource.Id is null
                ? null
                : existingSources.Find(s => s.Id.Equals(requestedSource.Id));

            if (existingSource is null)
            {
                await sourcesSet.AddAsync(ProductSource.CreateWithNoLinking(updatedEntity.Id, requestedSource.AgentId,
                    requestedSource.AgentArgument, date: updatedEntity.LastModified), cancellationToken).ConfigureAwait(false);
            }
            else if (!existingSource.AgentId.Equals(requestedSource.AgentId)
                || !existingSource.AgentArgument.Equals(requestedSource.AgentArgument, StringComparison.Ordinal))
            {
                GetContext().Entry(existingSource).CurrentValues.SetValues(existingSource with
                {
                    AgentId = requestedSource.AgentId,
                    AgentArgument = requestedSource.AgentArgument,
                    LastModified = updatedEntity.LastModified,
                });
            }
        }
    }

    public async Task<IEnumerable<ProductOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var products = await GetContext().Set<Product>()
            .AsNoTracking()
            .Include(p => p.Sources)
            .ThenInclude(s => s.Prices)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return products.Select(BuildOverview).ToArray();
    }

    public async Task<ProductOverviewDto?> GetOverviewByIdAsync(ProductRef productId, CancellationToken cancellationToken = default)
    {
        var product = await GetContext().Set<Product>()
            .AsNoTracking()
            .Include(p => p.Sources)
            .ThenInclude(s => s.Prices)
            .FirstOrDefaultAsync(ProductWithId(productId), cancellationToken).ConfigureAwait(false);

        return product is null ? null : BuildOverview(product);
    }

    public async Task<IEnumerable<ScanProduct>> GetScanProductsAsync(IEnumerable<ProductRef>? productIds = null, CancellationToken cancellationToken = default)
    {
        var products = GetContext().Set<Product>()
            .AsNoTracking()
            .Include(p => p.Sources)
            .ThenInclude(s => s.Agent)
            .AsQueryable();

        if (productIds is not null)
        {
            var ids = productIds.ToArray();
            products = products.Where(p => ids.Contains(p.Id));
        }

        var loadedProducts = await products.ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return loadedProducts.Select(p => new ScanProduct(
            p.Id.Id,
            p.Name,
            p.Sources.Select(s => new ScanSource(
                s.Id.Id,
                s.AgentArgument,
                new ScanAgent(s.Agent.Key, s.Agent.Url, s.Agent.PricePattern, s.Agent.Handler, s.Agent.DecimalDelimiter)))
                .ToArray()))
            .ToArray();
    }

    public async Task<IEnumerable<ProductPriceDto>> GetPricesAsync(ProductRef productId, CancellationToken cancellationToken = default)
    {
        var product = await GetContext().Set<Product>()
            .AsNoTracking()
            .Include(p => p.Sources)
            .ThenInclude(s => s.Prices)
            .FirstOrDefaultAsync(ProductWithId(productId), cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return [];
        }

        return product.Sources
            .SelectMany(s => s.Prices)
            .OrderBy(p => p.FoundDate)
            .Select(p => new ProductPriceDto(p.Id, p.ProductSourceId, p.Status, p.Price, p.FoundDate))
            .ToArray();
    }

    public async Task AddScanResultsAsync(ProductRef productId, IReadOnlyCollection<PriceSeekResult> results, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(results);

        var foundDate = _dateTime.NowUtc;
        var pricesSet = GetContext().Set<ProductPrice>();
        foreach (var result in results)
        {
            await pricesSet.AddAsync(ProductPrice.CreateWithNoLinking(result.ProductSourceId, result.Status, result.Price,
                foundDate, date: foundDate), cancellationToken).ConfigureAwait(false);
        }

        await GetContext().SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DropPricesAsync(ProductRef productId, CancellationToken cancellationToken = default)
    {
        var product = await GetContext().Set<Product>()
            .Include(p => p.Sources)
            .ThenInclude(s => s.Prices)
            .FirstOrDefaultAsync(ProductWithId(productId), cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID '{productId}' not found.");
        }

        var pricesSet = GetContext().Set<ProductPrice>();
        foreach (var price in product.Sources.SelectMany(s => s.Prices))
        {
            pricesSet.Remove(price);
        }

        await GetContext().SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private ProductOverviewDto BuildOverview(Product product)
    {
        var allPrices = product.Sources.SelectMany(s => s.Prices).ToArray();
        var latestPerSource = product.Sources
            .Select(s => s.Prices.OrderByDescending(p => p.FoundDate).FirstOrDefault())
            .Where(p => p is not null)
            .Select(p => p!)
            .ToArray();

        var successfulPrices = allPrices
            .Where(p => p.Status == AgentHandlingStatus.Success && p.Price is not null)
            .ToArray();
        var lowest = successfulPrices.Length == 0
            ? null
            : successfulPrices.MinBy(p => p.Price);

        var recentSuccessfulPrices = latestPerSource
            .Where(p => p.Status == AgentHandlingStatus.Success && p.Price is not null)
            .ToArray();
        var recentPrice = recentSuccessfulPrices.Length == 0
            ? (decimal?)null
            : recentSuccessfulPrices.Min(p => p.Price);

        var status = _statusProvider.DetermineStatus(
            latestPerSource.Select(p => new PriceSnapshot(p.Status, p.Price, p.FoundDate)).ToArray());

        return new ProductOverviewDto(
            product.Id,
            product.Name,
            product.Category,
            product.Description,
            status,
            lowest?.Price,
            lowest?.FoundDate,
            recentPrice,
            allPrices.Length == 0 ? null : allPrices.Max(p => p.FoundDate),
            product.LastModified);
    }

    private static Expression<Func<Product, bool>> ProductWithId(ProductRef id)
    {
        var param = Expression.Parameter(typeof(Product), "p");
        var property = Expression.Property(param, nameof(Product.Id));
        var constant = Expression.Constant(id, typeof(ProductRef));
        return Expression.Lambda<Func<Product, bool>>(Expression.Equal(property, constant), param);
    }

    private static Expression<Func<ProductSource, bool>> BelongsToProduct(ProductRef id)
    {
        var param = Expression.Parameter(typeof(ProductSource), "s");
        var property = Expression.Property(param, nameof(ProductSource.ProductId));
        var constant = Expression.Constant(id, typeof(ProductRef));
        return Expression.Lambda<Func<ProductSource, bool>>(Expression.Equal(property, constant), param);
    }
}
