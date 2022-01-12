using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Services;

public class PriceSeekerTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ITrickyHttpClient> _httpMock = new();
    private readonly Mock<IFileService> _fileMock = new();
    private readonly Mock<ILogger<PriceSeeker>> _loggerMock = new();
    private readonly Mock<IAgentHandlersProvider> _agentHandlersProviderMock = new();
    private readonly Mock<IAgentHandler> _agentHandlerMock = new();

    private readonly PriceSeeker _sut;

    public PriceSeekerTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior(recursionDepth: 2));

        _agentHandlersProviderMock.Setup(x => x.FindByName(It.IsAny<string>()))
            .Returns(_agentHandlerMock.Object);

        _sut = new PriceSeeker(_httpMock.Object, _fileMock.Object, _agentHandlersProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SeekAsync__Happy_flow_scenario()
    {
        // Arrange
        var product = CreateSampleProduct();

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Equal(product.Sources.Length, result.Length);
        _fileMock.Verify(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Warning, Times.Never());
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Error, Times.Never());
    }

    [Fact]
    public async Task SeekAsync__Content_was_not_downloaded__Returns_bad_status()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        _httpMock.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.Equal(AgentHandlingStatus.CouldNotFetch, result[0].Status);
    }

    [Fact]
    public async Task SeekAsync__Content_not_matched_the_pattern__Returns_bad_status_and_dumps_file()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        decimal? price;
        _agentHandlerMock.Setup(x => x.Handle(It.IsAny<Agent>(), It.IsAny<string>(), out price))
            .Returns(AgentHandlingStatus.CouldNotMatch);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.Equal(AgentHandlingStatus.CouldNotMatch, result[0].Status);
        _fileMock.Verify(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Error);
    }

    [Fact]
    public async Task SeekAsync__Price_is_invalid__Returns_bad_status()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        decimal? price;
        _agentHandlerMock.Setup(x => x.Handle(It.IsAny<Agent>(), It.IsAny<string>(), out price))
            .Returns(AgentHandlingStatus.InvalidPrice);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.Equal(AgentHandlingStatus.InvalidPrice, result[0].Status);
    }

    [Fact]
    public async Task SeekAsync__Price_is_not_convertible__Returns_bad_status()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        decimal? price;
        _agentHandlerMock.Setup(x => x.Handle(It.IsAny<Agent>(), It.IsAny<string>(), out price))
            .Returns(AgentHandlingStatus.CouldNotParse);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.Equal(AgentHandlingStatus.CouldNotParse, result[0].Status);
    }

    private Product CreateSampleProduct(int sourcesCount = 3) //, char delimiter = '.')
    {
        var product = _fixture.Build<Product>()
            .With(x => x.Sources, _fixture.CreateMany<ProductSource>(sourcesCount).ToArray())
            .Create();
        foreach (var productSource in product.Sources)
        {
            productSource.Agent = _fixture.Build<Agent>()
                .With(x => x.Url, _fixture.Create<string>() + "{0}")
                .Create();
            var content = _fixture.Create<string>();

            _httpMock.Setup(x => x.DownloadContent(
                It.Is<string>(url => url == string.Format(productSource.Agent.Url, productSource.AgentArgument)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(content);
        }
        return product;
    }
}
