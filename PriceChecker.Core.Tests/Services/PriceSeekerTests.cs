using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Services;

public class PriceSeekerTests
{
    private readonly Fixture _fixture = new();
    private readonly ITrickyHttpClient _httpMock = A.Fake<ITrickyHttpClient>();
    private readonly IFileService _fileMock = A.Fake<IFileService>();
    private readonly FakeLogger<PriceSeeker> _logger = new FakeLogger<PriceSeeker>();
    private readonly IAgentHandlersProvider _agentHandlersProviderMock = A.Fake<IAgentHandlersProvider>();
    private readonly IAgentHandler _agentHandlerMock = A.Fake<IAgentHandler>();

    private readonly PriceSeeker _sut;

    public PriceSeekerTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior(recursionDepth: 2));

        A.CallTo(() => _agentHandlersProviderMock.FindByName(A<string>._))
            .Returns(_agentHandlerMock);

        _sut = new PriceSeeker(_httpMock, _fileMock, _agentHandlersProviderMock, _logger);
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
        A.CallTo(() => _fileMock.WriteTextToFile(A<string>._, A<string>._)).MustNotHaveHappened();
        Assert.DoesNotContain(_logger.Logs, x => x.LogLevel is LogLevel.Error or LogLevel.Warning);
    }

    [Fact]
    public async Task SeekAsync__Content_was_not_downloaded__Returns_bad_status()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        A.CallTo(() => _httpMock.DownloadContentAsync(A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult((string?)null));

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
        A.CallTo(() => _agentHandlerMock.Handle(A<Agent>._, A<string>._, out price))
            .Returns(AgentHandlingStatus.CouldNotMatch);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.Equal(AgentHandlingStatus.CouldNotMatch, result[0].Status);
                A.CallTo(() => _fileMock.WriteTextToFile(A<string>._, A<string>._)).MustHaveHappenedOnceExactly();
        Assert.Single(_logger.Logs, x => x.LogLevel is LogLevel.Error);
    }

    [Fact]
    public async Task SeekAsync__Price_is_invalid__Returns_bad_status()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        decimal? price;
        A.CallTo(() => _agentHandlerMock.Handle(A<Agent>._, A<string>._, out price))
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
        A.CallTo(() => _agentHandlerMock.Handle(A<Agent>._, A<string>._, out price))
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

            A.CallTo(() => _httpMock.DownloadContentAsync(
                A<string>.That.IsEqualTo(string.Format(productSource.Agent.Url, productSource.AgentArgument)),
                A<CancellationToken>._))
                .Returns(content);
        }
        return product;
    }
}
