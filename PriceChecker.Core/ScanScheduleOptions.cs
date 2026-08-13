namespace Genius.PriceChecker.Core;

/// <summary>
///   The configuration of the automatic price scan, bound from the <c>Scanning:Schedule</c> section.
///   The scan runs once a day, at <see cref="TimeOfDay"/> wall-clock time in <see cref="TimeZone"/>.
/// </summary>
public sealed record ScanScheduleOptions
{
    /// <summary>
    ///   Whether the automatic daily scan runs at all. Manual on-demand scans are unaffected.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    ///   The wall-clock time of the daily scan, in <see cref="TimeZone"/>.
    /// </summary>
    public TimeOnly TimeOfDay { get; init; } = new(9, 0);

    /// <summary>
    ///   The IANA (or Windows) identifier of the time zone the <see cref="TimeOfDay"/> is expressed in.
    ///   Explicit rather than machine-local, because the container clock is UTC.
    /// </summary>
    public string TimeZone { get; init; } = "Europe/Amsterdam";

    /// <summary>
    ///   How long after a scheduled scan a product may stay unscanned before it reads as outdated.
    ///   This is what keeps the products list from flipping to "Outdated" while the scan is still running.
    /// </summary>
    public TimeSpan OutdatedGrace { get; init; } = TimeSpan.FromHours(3);
}
