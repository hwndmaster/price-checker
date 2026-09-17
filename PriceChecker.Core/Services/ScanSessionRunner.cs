using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface IScanSessionRunner
{
    /// <summary>
    ///   Scans every source of the given products and reports the progress to <paramref name="observer"/>.
    /// </summary>
    Task RunAsync(IReadOnlyCollection<ScanProduct> products, IScanSessionObserver observer,
        CancellationToken cancellationToken);
}

/// <summary>
///   Runs one scanning session. The unit of work is the product source rather than the product,
///   because the sources of a single product are spread over several sites while a single site is
///   commonly shared by the sources of several products. All the sources of the session are therefore
///   regrouped into one queue per domain: the queues run in parallel, each queue runs sequentially and
///   pauses between its requests, and a product is reported as scanned once its last source has been,
///   whichever queue that was.
/// </summary>
internal sealed class ScanSessionRunner : IScanSessionRunner
{
    private readonly IPriceSeeker _priceSeeker;
    private readonly IDelayService _delayService;
    private readonly ScanPacingOptions _options;
    private readonly ILogger<ScanSessionRunner> _logger;

    public ScanSessionRunner(IPriceSeeker priceSeeker, IDelayService delayService,
        ScanPacingOptions options, ILogger<ScanSessionRunner> logger)
    {
        _priceSeeker = priceSeeker.NotNull();
        _delayService = delayService.NotNull();
        _options = options.NotNull();
        _logger = logger.NotNull();
    }

    public async Task RunAsync(IReadOnlyCollection<ScanProduct> products, IScanSessionObserver observer,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(products);
        Guard.NotNull(observer);

        var trackers = products.Select(product => new ProductScanTracker(product)).ToArray();
        var queues = trackers
            .SelectMany(tracker => tracker.Product.Sources.Select(source => (Tracker: tracker, Source: source)))
            .GroupBy(x => ScanDomain.Resolve(x.Source), StringComparer.Ordinal)
            .ToArray();

        _logger.LogInformation("Scanning {SourceCount} source(s) of {ProductCount} product(s), spread over {DomainCount} domain(s): {Domains}",
            queues.Sum(queue => queue.Count()), products.Count, queues.Length,
            string.Join(", ", queues.Select(queue => queue.Key)));

        // A product without sources has nothing to wait for and would never be reported otherwise,
        // leaving the session short of one product for good.
        foreach (var tracker in trackers.Where(x => x.Product.Sources.Length == 0))
        {
            await ReportAsync(token => observer.OnProductStartedAsync(tracker.Product, token),
                tracker.Product, cancellationToken).ConfigureAwait(false);
            await ReportAsync(token => observer.OnProductScannedAsync(tracker.Product, [], token),
                tracker.Product, cancellationToken).ConfigureAwait(false);
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxParallelDomains,
            CancellationToken = cancellationToken,
        };
        await Parallel.ForEachAsync(queues, parallelOptions,
            async (queue, token) => await ScanDomainQueueAsync(queue, observer, token).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    /// <summary>
    ///   Scans the sources of a single domain, one at a time and pausing in between, so that the site
    ///   is never hit twice in quick succession. The other domains run in parallel with this one.
    /// </summary>
    private async Task ScanDomainQueueAsync(IGrouping<string, (ProductScanTracker Tracker, ScanSource Source)> queue,
        IScanSessionObserver observer, CancellationToken cancellationToken)
    {
        var isFirst = true;
        foreach (var (tracker, source) in queue)
        {
            if (!isFirst)
            {
                var delay = NextDelay();
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogTrace("Pausing for {Delay} before the next request to '{Domain}'", delay, queue.Key);
                    await _delayService.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            isFirst = false;

            if (tracker.TryBeginScan())
            {
                _logger.LogTrace("Scanning product '{ProductName}'", tracker.Product.Name);
                await ReportAsync(token => observer.OnProductStartedAsync(tracker.Product, token),
                    tracker.Product, cancellationToken).ConfigureAwait(false);
            }

            var result = await SeekAsync(tracker.Product, source, cancellationToken).ConfigureAwait(false);

            if (tracker.TryCompleteProduct(result, out var results))
            {
                await ReportAsync(token => observer.OnProductScannedAsync(tracker.Product, results, token),
                    tracker.Product, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<PriceSeekResult> SeekAsync(ScanProduct product, ScanSource source, CancellationToken cancellationToken)
    {
        try
        {
            return await _priceSeeker.SeekAsync(source, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A failing source is recorded as one and stays confined to itself: the rest of this
            // domain queue still has to be scanned, and the other sources of the same product still
            // produce a price. An HTTP timeout surfaces as a cancellation of its own, which is why the
            // filter above tells it apart from a cancellation of the session.
            _logger.LogError(ex, "Scanning the '{AgentKey}' source of '{ProductName}' failed",
                source.Agent.Key, product.Name);
            return new PriceSeekResult(AgentHandlingStatus.CouldNotFetch, source.SourceId, source.Agent.Key, null);
        }
    }

    /// <summary>
    ///   Invokes an observer callback. What that callback does belongs to the caller, and a failure in
    ///   it must not take the rest of the domain queue down with it.
    /// </summary>
    private async Task ReportAsync(Func<CancellationToken, Task> report, ScanProduct product,
        CancellationToken cancellationToken)
    {
        try
        {
            await report(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reporting the scan of '{ProductName}' failed", product.Name);
        }
    }

    /// <summary>
    ///   The pause before the next request to the same domain, randomized around the configured delay
    ///   so that the requests do not arrive on an exact, machine-like beat.
    /// </summary>
    private TimeSpan NextDelay()
    {
        if (_options.SameDomainDelay <= TimeSpan.Zero || _options.SameDomainDelayJitter <= 0)
        {
            return _options.SameDomainDelay;
        }

        var deviation = ((Random.Shared.NextDouble() * 2) - 1) * _options.SameDomainDelayJitter;

        return _options.SameDomainDelay * (1 + deviation);
    }

    /// <summary>
    ///   Collects the results of one product while its sources are scanned from several domain queues
    ///   at once, and singles out the queue that completed it.
    /// </summary>
    private sealed class ProductScanTracker
    {
        private readonly Lock _lock = new();
        private readonly List<PriceSeekResult> _results;
        private bool _started;

        public ProductScanTracker(ScanProduct product)
        {
            Product = product;
            _results = new List<PriceSeekResult>(product.Sources.Length);
        }

        public ScanProduct Product { get; }

        /// <summary>
        ///   Returns <c>true</c> for the first caller only.
        /// </summary>
        public bool TryBeginScan()
        {
            lock (_lock)
            {
                if (_started)
                {
                    return false;
                }

                _started = true;
                return true;
            }
        }

        /// <summary>
        ///   Records the result of one source and returns <c>true</c> for the caller that recorded the
        ///   last one, handing it the complete set.
        /// </summary>
        public bool TryCompleteProduct(PriceSeekResult result, out IReadOnlyCollection<PriceSeekResult> results)
        {
            lock (_lock)
            {
                _results.Add(result);
                if (_results.Count < Product.Sources.Length)
                {
                    results = [];
                    return false;
                }

                results = _results.ToArray();
                return true;
            }
        }
    }
}
