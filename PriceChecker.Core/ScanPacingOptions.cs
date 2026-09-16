namespace Genius.PriceChecker.Core;

/// <summary>
///   The configuration of how a scanning session paces its requests, bound from the
///   <c>Scanning:Pacing</c> section. Every source is scanned through the domain it is fetched from:
///   different domains are scanned in parallel, a single domain never in parallel with itself, and
///   two requests to the same domain are always separated by a pause.
/// </summary>
public sealed record ScanPacingOptions
{
    /// <summary>
    ///   How long to wait after a request to a domain before the next request to that same domain.
    ///   The first request to a domain is never delayed. Zero disables the pacing, leaving only the
    ///   per-domain serialization.
    /// </summary>
    public TimeSpan SameDomainDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///   How far an individual pause may deviate from <see cref="SameDomainDelay"/>, as a fraction of
    ///   it: 0.2 turns a 30 second pause into a random one between 24 and 36 seconds. An exact,
    ///   metronome-like interval is a bot fingerprint in itself. Zero disables the randomisation.
    /// </summary>
    public double SameDomainDelayJitter { get; init; } = 0.2;

    /// <summary>
    ///   How many domains may be scanned in parallel. A domain is scanned by a single worker, so this
    ///   is also the upper bound of the concurrent outgoing requests of a scanning session.
    /// </summary>
    public int MaxParallelDomains { get; init; } = 8;

    /// <summary>
    ///   Throws when the bound configuration cannot produce a working pacing, so that a typo in
    ///   <c>appsettings.json</c> surfaces at startup rather than at the first scan.
    /// </summary>
    public void Validate()
    {
        if (SameDomainDelay < TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(SameDomainDelay)} cannot be negative, but is {SameDomainDelay}.");

        if (SameDomainDelayJitter is < 0 or >= 1)
            throw new InvalidOperationException($"{nameof(SameDomainDelayJitter)} must be within [0, 1), but is {SameDomainDelayJitter}.");

        if (MaxParallelDomains < 1)
            throw new InvalidOperationException($"{nameof(MaxParallelDomains)} must be at least 1, but is {MaxParallelDomains}.");
    }
}
