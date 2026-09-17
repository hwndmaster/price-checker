using System.Collections.Concurrent;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Dto;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class ScanSessionRunnerTests
{
    private const string SeekEvent = "seek";
    private const string DelayEvent = "delay";

    private readonly ConcurrentQueue<string> _timeline = new();
    private readonly TestTrackingPriceSeeker _priceSeeker;
    private readonly TestRecordingDelayService _delayService;
    private readonly TestRecordingObserver _observer = new();
    private readonly FakeLogger<ScanSessionRunner> _logger = new();

    public ScanSessionRunnerTests()
    {
        _priceSeeker = new TestTrackingPriceSeeker(_timeline);
        _delayService = new TestRecordingDelayService(_timeline);
    }

    [Fact]
    public async Task RunAsync_GivenSeveralSourcesOfOneDomain_WhenRun_ThenScansThemOneByOneWithAPauseInBetween()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "amazon.de", "amazon.de");
        var sut = CreateSut();

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        // A single domain gives a deterministic timeline: no pause before the first request, one
        // before every following one.
        Assert.Equal(
            [$"{SeekEvent}:amazon.de", DelayEvent, $"{SeekEvent}:amazon.de", DelayEvent, $"{SeekEvent}:amazon.de"],
            _timeline);
    }

    [Fact]
    public async Task RunAsync_GivenSourcesOfSeveralDomains_WhenRun_ThenNeverScansOneDomainTwiceAtOnce()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Roomba", "amazon.de", "tweakers.net"),
            CreateProduct("Dashcam", "amazon.de", "tweakers.net"),
            CreateProduct("Wheel cleaner", "amazon.de", "bol.com"),
        };
        var sut = CreateSut();

        // Act
        await sut.RunAsync(products, _observer, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(_priceSeeker.HasOverlappingDomains);
        Assert.Equal(6, _priceSeeker.SeekCount);
        // Three domains, six sources: every source but the first of each domain waited.
        Assert.Equal(3, _delayService.Delays.Count);
    }

    [Fact]
    public async Task RunAsync_GivenSourcesOfSeveralDomains_WhenRun_ThenScansTheDomainsInParallel()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Roomba", "amazon.de"),
            CreateProduct("Dashcam", "tweakers.net"),
            CreateProduct("Wheel cleaner", "bol.com"),
        };
        // Every request blocks until all three are in flight, so the session can only finish when the
        // three domains are indeed scanned at the same time.
        _priceSeeker.ExpectParallelCalls(3);
        var sut = CreateSut();

        // Act
        var run = sut.RunAsync(products, _observer, TestContext.Current.CancellationToken);

        // Assert
        await run.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        Assert.Equal(3, _observer.Scanned.Count);
    }

    [Fact]
    public async Task RunAsync_GivenAPacingDelay_WhenRun_ThenRandomisesItAroundTheConfiguredValue()
    {
        // Arrange
        var product = CreateProduct("Roomba", Enumerable.Repeat("amazon.de", 20).ToArray());
        var sut = CreateSut(new ScanPacingOptions
        {
            SameDomainDelay = TimeSpan.FromSeconds(30),
            SameDomainDelayJitter = 0.2,
        });

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(19, _delayService.Delays.Count);
        Assert.All(_delayService.Delays,
            delay => Assert.InRange(delay, TimeSpan.FromSeconds(24), TimeSpan.FromSeconds(36)));
        // An exact interval would be a fingerprint of its own.
        Assert.True(_delayService.Delays.Distinct().Count() > 1);
    }

    [Fact]
    public async Task RunAsync_GivenNoJitterConfigured_WhenRun_ThenPausesExactlyTheConfiguredDelay()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "amazon.de", "amazon.de");
        var sut = CreateSut(new ScanPacingOptions
        {
            SameDomainDelay = TimeSpan.FromSeconds(45),
            SameDomainDelayJitter = 0,
        });

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(45)], _delayService.Delays);
    }

    [Fact]
    public async Task RunAsync_GivenNoPacingDelayConfigured_WhenRun_ThenDoesNotPauseButStillSerialisesTheDomain()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "amazon.de", "amazon.de");
        var sut = CreateSut(new ScanPacingOptions { SameDomainDelay = TimeSpan.Zero });

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(_delayService.Delays);
        Assert.False(_priceSeeker.HasOverlappingDomains);
    }

    [Fact]
    public async Task RunAsync_GivenAProductSpreadOverDomains_WhenRun_ThenReportsItStartedOnceAndScannedWithEveryResult()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "tweakers.net", "bol.com");
        var sut = CreateSut();

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        // Three domain queues reach the same product, but it starts and finishes exactly once.
        Assert.Equal([product.ProductId], _observer.Started.Select(x => x.ProductId));
        var results = Assert.Single(_observer.Scanned).Value;
        Assert.Equal(product.Sources.Select(x => x.SourceId).Order(),
            results.Select(x => x.ProductSourceId).Order());
    }

    [Fact]
    public async Task RunAsync_GivenAFailingSource_WhenRun_ThenRecordsItAsNotFetchedAndScansTheRest()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "amazon.de", "amazon.de");
        var failing = product.Sources[0];
        _priceSeeker.FailOn(failing, new HttpRequestException("the host is unreachable"));
        var sut = CreateSut();

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        // The failure stays confined to its own source: the queue behind it is still scanned, and the
        // product is still reported with a result for every source.
        var results = Assert.Single(_observer.Scanned).Value;
        Assert.Equal(3, results.Count);
        Assert.Equal(AgentHandlingStatus.CouldNotFetch,
            results.Single(x => x.ProductSourceId == failing.SourceId).Status);
        Assert.Equal(2, results.Count(x => x.Status == AgentHandlingStatus.Success));
        Assert.Single(_logger.Logs, x => x.LogLevel == LogLevel.Error);
    }

    [Fact]
    public async Task RunAsync_GivenASourceTimingOut_WhenRun_ThenRecordsItAsNotFetched()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "amazon.de");
        // An HTTP timeout surfaces as a cancellation although the session itself was never cancelled.
        _priceSeeker.FailOn(product.Sources[0], new TaskCanceledException("the request timed out"));
        var sut = CreateSut();

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        var results = Assert.Single(_observer.Scanned).Value;
        Assert.Equal(2, results.Count);
        Assert.Equal(AgentHandlingStatus.CouldNotFetch,
            results.Single(x => x.ProductSourceId == product.Sources[0].SourceId).Status);
    }

    [Fact]
    public async Task RunAsync_GivenACancelledSession_WhenRun_ThenStopsScanning()
    {
        // Arrange
        var product = CreateProduct("Roomba", "amazon.de", "tweakers.net");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.RunAsync([product], _observer, cancellation.Token));
        Assert.Equal(0, _priceSeeker.SeekCount);
    }

    [Fact]
    public async Task RunAsync_GivenAnObserverThatFails_WhenRun_ThenKeepsScanningTheQueue()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Roomba", "amazon.de"),
            CreateProduct("Dashcam", "amazon.de"),
        };
        _observer.ThrowOnStarted = new InvalidOperationException("the notification failed");
        var sut = CreateSut();

        // Act
        await sut.RunAsync(products, _observer, TestContext.Current.CancellationToken);

        // Assert
        // What the observer does with the report is the caller's business; it cannot cost the session
        // the rest of the queue.
        Assert.Equal(2, _priceSeeker.SeekCount);
        Assert.Equal(2, _observer.Scanned.Count);
        Assert.Equal(2, _logger.Logs.Count(x => x.LogLevel == LogLevel.Error));
    }

    [Fact]
    public async Task RunAsync_GivenAProductWithoutSources_WhenRun_ThenReportsItAsScannedWithNoResults()
    {
        // Arrange
        var product = CreateProduct("Roomba");
        var sut = CreateSut();

        // Act
        await sut.RunAsync([product], _observer, TestContext.Current.CancellationToken);

        // Assert
        // It has nothing to wait for, and would otherwise leave the session one product short forever.
        Assert.Equal([product.ProductId], _observer.Started.Select(x => x.ProductId));
        Assert.Empty(Assert.Single(_observer.Scanned).Value);
    }

    private ScanSessionRunner CreateSut(ScanPacingOptions? options = null)
        => new(_priceSeeker, _delayService, options ?? new ScanPacingOptions(), _logger);

    private static ScanProduct CreateProduct(string name, params string[] domains)
        => new(Guid.NewGuid(), name,
            domains.Select(domain => new ScanSource(Guid.NewGuid(), "argument",
                new ScanAgent(domain, $"https://www.{domain}/product/{{0}}", "pattern", "SimpleRegex", '.')))
                .ToArray());

    /// <summary>
    ///   Stands in for the real seeker, recording what was scanned when and flagging any two sources of
    ///   one domain that were in flight at the same time.
    /// </summary>
    private sealed class TestTrackingPriceSeeker : IPriceSeeker
    {
        private readonly ConcurrentQueue<string> _timeline;
        private readonly ConcurrentDictionary<string, int> _inFlight = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<Guid, Exception> _failures = new();
        private TaskCompletionSource? _rendezvous;
        private int _rendezvousExpected;
        private int _rendezvousArrived;
        private int _overlapping;
        private int _seekCount;

        public TestTrackingPriceSeeker(ConcurrentQueue<string> timeline)
        {
            _timeline = timeline;
        }

        public bool HasOverlappingDomains => Volatile.Read(ref _overlapping) == 1;
        public int SeekCount => Volatile.Read(ref _seekCount);

        public void FailOn(ScanSource source, Exception exception) => _failures[source.SourceId] = exception;

        /// <summary>
        ///   Holds every call until the expected number of them is in flight at once.
        /// </summary>
        public void ExpectParallelCalls(int expected)
        {
            _rendezvousExpected = expected;
            _rendezvous = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<PriceSeekResult> SeekAsync(ScanSource productSource, CancellationToken cancel)
        {
            var domain = ScanDomain.Resolve(productSource);
            _timeline.Enqueue($"{SeekEvent}:{domain}");
            Interlocked.Increment(ref _seekCount);

            if (_inFlight.AddOrUpdate(domain, 1, (_, count) => count + 1) > 1)
            {
                Interlocked.Exchange(ref _overlapping, 1);
            }

            try
            {
                await Task.Yield();

                if (_rendezvous is not null)
                {
                    if (Interlocked.Increment(ref _rendezvousArrived) >= _rendezvousExpected)
                    {
                        _rendezvous.TrySetResult();
                    }

                    await _rendezvous.Task;
                }
            }
            finally
            {
                _inFlight.AddOrUpdate(domain, 0, (_, count) => count - 1);
            }

            if (_failures.TryGetValue(productSource.SourceId, out var failure))
            {
                throw failure;
            }

            return new PriceSeekResult(AgentHandlingStatus.Success, productSource.SourceId, productSource.Agent.Key, 9.99m);
        }
    }

    private sealed class TestRecordingDelayService : IDelayService
    {
        private readonly ConcurrentQueue<string> _timeline;
        private readonly ConcurrentQueue<TimeSpan> _delays = new();

        public TestRecordingDelayService(ConcurrentQueue<string> timeline)
        {
            _timeline = timeline;
        }

        public IReadOnlyCollection<TimeSpan> Delays => _delays.ToArray();

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            _timeline.Enqueue(DelayEvent);
            _delays.Enqueue(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class TestRecordingObserver : IScanSessionObserver
    {
        private readonly ConcurrentQueue<ScanProduct> _started = new();
        private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<PriceSeekResult>> _scanned = new();

        public Exception? ThrowOnStarted { get; set; }

        public IReadOnlyCollection<ScanProduct> Started => _started.ToArray();
        public IReadOnlyDictionary<Guid, IReadOnlyCollection<PriceSeekResult>> Scanned => _scanned;

        public Task OnProductStartedAsync(ScanProduct product, CancellationToken cancellationToken)
        {
            _started.Enqueue(product);

            return ThrowOnStarted is null ? Task.CompletedTask : Task.FromException(ThrowOnStarted);
        }

        public Task OnProductScannedAsync(ScanProduct product, IReadOnlyCollection<PriceSeekResult> results,
            CancellationToken cancellationToken)
        {
            _scanned[product.ProductId] = results;

            return Task.CompletedTask;
        }
    }
}
