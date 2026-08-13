using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;

namespace Genius.PriceChecker.Db.Tests.Repositories;

public sealed class ProductsRepositoryTests
{
    private readonly FakeDateTime _dateTime = new();
    private readonly IProductStatusProvider _statusProviderMock = A.Fake<IProductStatusProvider>();

    [Fact]
    public async Task CreateAsync_GivenRequestWithSources_WhenCreated_ThenProductAndSourcesAreStored()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var (productsRepository, agentId) = await CreateSystemUnderTestAsync(context);

        // Act
        var created = await productsRepository.CreateAsync(
            new CreateProductRequest("Test Product", "Category", null,
                [new CreateProductSourceRequest(agentId, "B000123")]),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var product = await productsRepository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Equal("Test Product", product.Name);
        var source = Assert.Single(product.Sources);
        Assert.Equal(agentId, source.AgentId);
        Assert.Equal("B000123", source.AgentArgument);
    }

    [Fact]
    public async Task UpdateAsync_GivenChangedSources_WhenUpdated_ThenSourcesAreSynchronized()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var (productsRepository, agentId) = await CreateSystemUnderTestAsync(context);
        var created = await productsRepository.CreateAsync(
            new CreateProductRequest("Test Product", null, null,
                [
                    new CreateProductSourceRequest(agentId, "kept-and-updated"),
                    new CreateProductSourceRequest(agentId, "removed"),
                ]),
            cancellationToken: TestContext.Current.CancellationToken);
        var product = await productsRepository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        var keptSource = product.Sources.First(x => x.AgentArgument == "kept-and-updated");

        // Act: update one source, remove another one, add a new one
        _dateTime.Advance(TimeSpan.FromMinutes(5));
        await productsRepository.UpdateAsync(
            new UpdateProductRequest(created.EntityId, created.LastModified, "Test Product", null, null,
                [
                    new UpdateProductSourceRequest(keptSource.Id, agentId, "updated-argument"),
                    new UpdateProductSourceRequest(null, agentId, "added"),
                ]),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        product = await productsRepository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Equal(2, product.Sources.Length);
        Assert.Equal("updated-argument", product.Sources.First(x => x.Id.Equals(keptSource.Id)).AgentArgument);
        Assert.Contains(product.Sources, x => x.AgentArgument == "added");
        Assert.DoesNotContain(product.Sources, x => x.AgentArgument == "removed");
    }

    [Fact]
    public async Task AddScanResultsAsync_GivenSeekResults_WhenAddedAndDropped_ThenOverviewReflectsThePrices()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var (productsRepository, agentId) = await CreateSystemUnderTestAsync(context);
        var created = await productsRepository.CreateAsync(
            new CreateProductRequest("Test Product", null, null,
                [new CreateProductSourceRequest(agentId, "B000123")]),
            cancellationToken: TestContext.Current.CancellationToken);
        var product = await productsRepository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        var sourceId = product.Sources[0].Id;

        // Act
        await productsRepository.AddScanResultsAsync(created.EntityId,
            [new PriceSeekResult(AgentHandlingStatus.Success, sourceId.Id, "agent", 99.95m)],
            TestContext.Current.CancellationToken);

        // Assert
        var overview = await productsRepository.GetOverviewByIdAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.NotNull(overview);
        Assert.Equal(99.95m, overview.LowestPrice);
        Assert.Equal(99.95m, overview.RecentPrice);
        var price = Assert.Single(await productsRepository.GetPricesAsync(created.EntityId, TestContext.Current.CancellationToken));
        Assert.Equal(sourceId, price.ProductSourceId);

        // Act & Assert: DropPrices
        await productsRepository.DropPricesAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Empty(await productsRepository.GetPricesAsync(created.EntityId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetScanProductsAsync_GivenProductWithSource_WhenFetched_ThenScanModelIsComplete()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var (productsRepository, agentId) = await CreateSystemUnderTestAsync(context);
        var created = await productsRepository.CreateAsync(
            new CreateProductRequest("Test Product", null, null,
                [new CreateProductSourceRequest(agentId, "B000123")]),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var scanProducts = await productsRepository.GetScanProductsAsync([created.EntityId], TestContext.Current.CancellationToken);

        // Assert
        var scanProduct = Assert.Single(scanProducts);
        Assert.Equal("Test Product", scanProduct.Name);
        var scanSource = Assert.Single(scanProduct.Sources);
        Assert.Equal("B000123", scanSource.Argument);
        Assert.Equal("test-agent", scanSource.Agent.Key);
        Assert.Equal('.', scanSource.Agent.DecimalDelimiter);
    }

    private async Task<(ProductsRepository ProductsRepository, AgentRef AgentId)> CreateSystemUnderTestAsync(RepositoryTestContext context)
    {
        var agentsRepository = new AgentsRepository(_dateTime, context);
        var createdAgent = await agentsRepository.CreateAsync(
            new CreateAgentRequest("test-agent", "https://example.com/{0}", "pattern", "SimpleRegex", "."));

        var productsRepository = new ProductsRepository(_dateTime, context, _statusProviderMock);
        return (productsRepository, createdAgent.EntityId);
    }
}
