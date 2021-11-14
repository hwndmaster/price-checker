using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Genius.PriceChecker.Core.Tests.Services;

public class PriceSeekerTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ITrickyHttpClient> _httpMock = new();
    private readonly Mock<IFileService> _fileMock = new();
    private readonly Mock<ILogger<PriceSeeker>> _loggerMock = new();

    private readonly PriceSeeker _sut;

    public PriceSeekerTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior(recursionDepth: 2));

        _sut = new PriceSeeker(_httpMock.Object, _fileMock.Object, _loggerMock.Object);
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
    public async Task SeekAsync__Content_wasnt_downloaded__Returns_null()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        _httpMock.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Empty(result);
    }

    [Fact]
    public async Task SeekAsync__Content_not_matched_the_pattern__Returns_null_and_dumps_file()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        var contentNotMatchingAnything = _fixture.Create<string>();
        _httpMock.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentNotMatchingAnything);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Empty(result);
        _fileMock.Verify(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Error);
    }

    [Fact]
    public async Task SeekAsync__DecimalDelimiter_isnt_default__Considered_in_price_parse()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1, delimiter: ';');

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Single(result);
        Assert.NotEqual(0, result[0].Price);
        Assert.NotEqual((int)result[0].Price, result[0].Price); // Check if it is decimal
    }

    [Fact]
    public async Task SeekAsync__Price_is_invalid__Returns_null()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        var contentPriceInvalid = "`0`";
        _httpMock.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentPriceInvalid);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Empty(result);
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Warning);
    }

    [Fact]
    public async Task SeekAsync__Price_isnt_convertible__Returns_null()
    {
        // Arrange
        var product = CreateSampleProduct(sourcesCount: 1);
        var contentPriceInvalid = "`not-a-number`";
        _httpMock.Setup(x => x.DownloadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentPriceInvalid);

        // Act
        var result = await _sut.SeekAsync(product, new CancellationToken());

        // Verify
        Assert.Empty(result);
        TestHelpers.VerifyLogger(_loggerMock, LogLevel.Error);
    }

    private Product CreateSampleProduct(int sourcesCount = 3, char delimiter = '.')
    {
        var product = _fixture.Build<Product>()
            .With(x => x.Sources, _fixture.CreateMany<ProductSource>(sourcesCount).ToArray())
            .Create();
        foreach (var productSource in product.Sources)
        {
            productSource.Agent = _fixture.Build<Agent>()
                .With(x => x.Url, _fixture.Create<string>() + "{0}")
                .With(x => x.PricePattern, $@"`(?<price>[\d\{delimiter}]+)`")
                .With(x => x.DecimalDelimiter, delimiter)
                .Create();

            var priceDec = _fixture.Create<int>();
            var priceFlt = _fixture.Create<int>();
            var content = $"{_fixture.Create<string>()}`{priceDec}{delimiter}{priceFlt}`{_fixture.Create<string>()}";

            _httpMock.Setup(x => x.DownloadContent(
                It.Is<string>(url => url == string.Format(productSource.Agent.Url, productSource.AgentArgument)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(content);
        }
        return product;
    }
}
