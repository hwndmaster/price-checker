using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   Receives the outcome of a scanning session as it unfolds. Implemented by the caller, which owns
///   the persistence and the notifications; the session runner itself knows about neither.
/// </summary>
public interface IScanSessionObserver
{
    /// <summary>
    ///   Invoked once per product, right before the first of its sources is scanned. A product may
    ///   start long after the session did, when its first source is queued behind others on the same
    ///   domain, and products do not start in any particular order.
    /// </summary>
    Task OnProductStartedAsync(ScanProduct product, CancellationToken cancellationToken);

    /// <summary>
    ///   Invoked once per product, when every one of its sources has been scanned. The results hold
    ///   one entry per source, in no particular order.
    /// </summary>
    Task OnProductScannedAsync(ScanProduct product, IReadOnlyCollection<PriceSeekResult> results,
        CancellationToken cancellationToken);
}
