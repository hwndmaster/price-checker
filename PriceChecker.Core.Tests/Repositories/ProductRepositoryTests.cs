using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Repositories;

public class ProductRepositoryTests
{
    private readonly ProductRepository _sut;
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<IJsonPersister> _persisterMock = new();
    private readonly Mock<IAgentQueryService> _agentQueryMock = new();

    private readonly Product[] _products;
    private readonly Agent[] _agents;

    public ProductRepositoryTests()
    {
        _products = ModelHelpers.SampleManyProducts().ToArray();
        _agents = ModelHelpers.SampleManyAgents(_products).ToArray();

        foreach (var agent in _agents)
            _agentQueryMock.Setup(x => x.FindByKey(agent.Key)).Returns(agent);

        _persisterMock.Setup(x => x.LoadCollection<Product>(It.IsAny<string>()))
            .Returns(_products);

        _sut = new ProductRepository(_eventBusMock.Object, _persisterMock.Object,
            _agentQueryMock.Object,
            Mock.Of<ILogger<ProductRepository>>());

        _sut.GetAllAsync().GetAwaiter().GetResult(); // To trigger the initializer
    }

    [Fact]
    public async Task GetAll__Returns_all_loaded_products()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Verify
        Assert.Equal(_products, result);
    }

    [Fact]
    public async Task FindById__Returns_appropriate_product()
    {
        // Arrange
        var productToFind = _products[1];

        // Act
        var result = await _sut.FindByIdAsync(productToFind.Id);

        // Verify
        Assert.Equal(productToFind, result);
    }

    [Fact]
    public async Task Delete__Removes_appripriate_product()
    {
        // Arrange
        var productToDelete = _products[1];

        // Act
        _sut.Delete(productToDelete.Id);

        // Verify
        Assert.Null(await _sut.FindByIdAsync(productToDelete.Id));
    }

    [Fact]
    public async Task Delete__When_no_product_found__Breaks_operation()
    {
        // Arrange
        var productCount = (await _sut.GetAllAsync()).Count();

        // Act
        _sut.Delete(Guid.NewGuid());

        // Verify
        Assert.Equal(productCount, (await _sut.GetAllAsync()).Count());
    }

    [Fact]
    public void Store__For_existing_product__Saves_it_and_fires_event()
    {
        // Arrange
        var product = _products[1];

        // Act
        _sut.Store(product);

        // Verify
        _persisterMock.Verify(x => x.Store(It.IsAny<string>(),
            It.Is((List<Product> p) => p.SequenceEqual(_products))));
        _eventBusMock.Verify(x => x.Publish(It.Is<EntitiesUpdatedEvent>(e => e.Entities.First() == product.Id)), Times.Once);
    }

    [Fact]
    public async Task Store__For_nonexisting_product__Adds_it_and_fires_event()
    {
        // Arrange
        var product = ModelHelpers.SampleProduct(_agents);
        var productCount = (await _sut.GetAllAsync()).Count();

        // Act
        _sut.Store(product);

        // Verify
        var expectedProducts = _products.Concat(new [] { product });
        _persisterMock.Verify(x => x.Store(It.IsAny<string>(),
            It.Is((List<Product> p) => p.SequenceEqual(expectedProducts))));
        _eventBusMock.Verify(x => x.Publish(It.Is<EntitiesAddedEvent>(e => e.Entities.First() == product.Id)), Times.Once);
        Assert.Equal(productCount + 1, (await _sut.GetAllAsync()).Count());
    }

    [Fact]
    public async Task Store__When_id_is_empty__Adds_product_with_autogenerated_id()
    {
        // Arrange
        var product = ModelHelpers.SampleProduct(_agents);
        product.Id = Guid.Empty;
        var productCount = (await _sut.GetAllAsync()).Count();

        // Act
        _sut.Store(product);

        // Verify
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(productCount + 1, (await _sut.GetAllAsync()).Count());
    }

    [Fact]
    public void Constructor__Loads_and_fills_up_relations()
    {
        // Act (done in the test constructor)

        // Verify
        foreach (var product in _products)
        {
            foreach (var source in product.Sources)
            {
                Assert.Equal(source.AgentKey, source.Agent.Key);
                Assert.Equal(product, source.Product);
            }
            foreach (var price in product.Recent)
            {
                Assert.Equal(product.Sources.First(x => x.Id == price.ProductSourceId), price.ProductSource);
            }
        }
    }
}
