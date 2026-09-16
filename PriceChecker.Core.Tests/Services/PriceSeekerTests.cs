using System.Text;
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
        var source = CreateSampleSource();
        decimal? price;
        A.CallTo(() => _agentHandlerMock.Handle(A<ScanAgent>._, A<string>._, out price))
            .Returns(AgentHandlingStatus.Success);

        // Act
        var result = await _sut.SeekAsync(source, new CancellationToken());

        // Assert
        Assert.Equal(AgentHandlingStatus.Success, result.Status);
        Assert.Equal(source.SourceId, result.ProductSourceId);
        A.CallTo(() => _fileMock.WriteTextToFileAsync(A<string>._, A<string>._, A<Encoding>._, A<CancellationToken?>._)).MustNotHaveHappened();
        Assert.DoesNotContain(_logger.Logs, x => x.LogLevel is LogLevel.Error or LogLevel.Warning);
    }

    [Fact]
    public async Task SeekAsync__Content_was_not_downloaded__Returns_bad_status()
    {
        // Arrange
        var source = CreateSampleSource();
        A.CallTo(() => _httpMock.DownloadContentAsync(A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult((string?)null));

        // Act
        var result = await _sut.SeekAsync(source, new CancellationToken());

        // Assert
        Assert.Equal(AgentHandlingStatus.CouldNotFetch, result.Status);
    }

    [Fact]
    public async Task SeekAsync__Content_could_not_be_downloaded__Throws()
    {
        // Arrange
        var source = CreateSampleSource();
        A.CallTo(() => _httpMock.DownloadContentAsync(A<string>._, A<CancellationToken>._))
            .Throws(new HttpRequestException("the host is unreachable"));

        // Act & Assert
        // Reporting it by throwing is what lets the session runner decide whether the rest of the
        // domain queue is still worth scanning.
        await Assert.ThrowsAsync<HttpRequestException>(() => _sut.SeekAsync(source, new CancellationToken()));
        Assert.Single(_logger.Logs, x => x.LogLevel is LogLevel.Error);
    }

    [Fact]
    public async Task SeekAsync__Content_not_matched_the_pattern__Returns_bad_status_and_dumps_file()
    {
        // Arrange
        var source = CreateSampleSource();
        decimal? price;
        A.CallTo(() => _agentHandlerMock.Handle(A<ScanAgent>._, A<string>._, out price))
            .Returns(AgentHandlingStatus.CouldNotMatch);

        // Act
        var result = await _sut.SeekAsync(source, new CancellationToken());

        // Assert
        Assert.Equal(AgentHandlingStatus.CouldNotMatch, result.Status);
        A.CallTo(() => _fileMock.WriteTextToFileAsync(A<string>._, A<string>._, A<Encoding>._, A<CancellationToken?>._)).MustHaveHappenedOnceExactly();
        Assert.Single(_logger.Logs, x => x.LogLevel is LogLevel.Error);
    }

    [Fact]
    public async Task SeekAsync__Price_is_invalid__Returns_bad_status()
    {
        // Arrange
        var source = CreateSampleSource();
        decimal? price;
        A.CallTo(() => _agentHandlerMock.Handle(A<ScanAgent>._, A<string>._, out price))
            .Returns(AgentHandlingStatus.InvalidPrice);

        // Act
        var result = await _sut.SeekAsync(source, new CancellationToken());

        // Assert
        Assert.Equal(AgentHandlingStatus.InvalidPrice, result.Status);
    }

    [Fact]
    public async Task SeekAsync__Price_is_not_convertible__Returns_bad_status()
    {
        // Arrange
        var source = CreateSampleSource();
        decimal? price;
        A.CallTo(() => _agentHandlerMock.Handle(A<ScanAgent>._, A<string>._, out price))
            .Returns(AgentHandlingStatus.CouldNotParse);

        // Act
        var result = await _sut.SeekAsync(source, new CancellationToken());

        // Assert
        Assert.Equal(AgentHandlingStatus.CouldNotParse, result.Status);
    }

    private ScanSource CreateSampleSource()
    {
        var agent = _fixture.Build<ScanAgent>()
            .With(x => x.Url, _fixture.Create<string>() + "{0}")
            .Create();
        var source = _fixture.Build<ScanSource>()
            .With(x => x.Agent, agent)
            .Create();

        A.CallTo(() => _httpMock.DownloadContentAsync(
            A<string>.That.IsEqualTo(string.Format(source.Agent.Url, source.Argument)),
            A<CancellationToken>._))
            .Returns(_fixture.Create<string>());

        return source;
    }
}
