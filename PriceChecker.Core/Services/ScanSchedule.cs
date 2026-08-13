namespace Genius.PriceChecker.Core.Services;

/// <summary>
///   The single source of truth for the automatic scan schedule. Both the background scanner and
///   the product status rule resolve against the same trigger, so they can never contradict
///   each other (e.g. report a product as outdated while the next scan is hours away).
/// </summary>
public interface IScanSchedule
{
    /// <summary>
    ///   Whether the automatic daily scan runs at all.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    ///   How long after a trigger a product may stay unscanned before it reads as outdated.
    /// </summary>
    TimeSpan OutdatedGrace { get; }

    /// <summary>
    ///   Returns the most recent scheduled scan moment at or before <paramref name="nowUtc"/>:
    ///   today's trigger once it has passed, yesterday's otherwise.
    /// </summary>
    DateTimeOffset GetMostRecentTrigger(DateTimeOffset nowUtc);
}

internal sealed class ScanSchedule : IScanSchedule
{
    private readonly TimeOnly _timeOfDay;
    private readonly TimeZoneInfo _timeZone;

    public ScanSchedule(ScanScheduleOptions options)
    {
        Guard.NotNull(options);

        Enabled = options.Enabled;
        OutdatedGrace = options.OutdatedGrace;
        _timeOfDay = options.TimeOfDay;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
    }

    public bool Enabled { get; }
    public TimeSpan OutdatedGrace { get; }

    public DateTimeOffset GetMostRecentTrigger(DateTimeOffset nowUtc)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, _timeZone).DateTime);
        var todaysTrigger = TriggerOn(localDate);

        return todaysTrigger <= nowUtc
            ? todaysTrigger
            : TriggerOn(localDate.AddDays(-1));
    }

    /// <summary>
    ///   Resolves the configured wall-clock time on the given local date into an absolute moment.
    /// </summary>
    private DateTimeOffset TriggerOn(DateOnly localDate)
    {
        var local = DateTime.SpecifyKind(localDate.ToDateTime(_timeOfDay), DateTimeKind.Unspecified);

        // The wall-clock time may not exist on the day the clocks jump forward. Skipping ahead by the
        // gap keeps the trigger on the intended day instead of throwing.
        if (_timeZone.IsInvalidTime(local))
        {
            local = local.Add(_timeZone.GetAdjustmentRules()
                .FirstOrDefault(rule => localDate >= DateOnly.FromDateTime(rule.DateStart)
                    && localDate <= DateOnly.FromDateTime(rule.DateEnd))
                ?.DaylightDelta ?? TimeSpan.FromHours(1));
        }

        // For the ambiguous hour when the clocks jump back, GetUtcOffset resolves to standard time,
        // which is deterministic and good enough for a once-a-day trigger.
        return new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
    }
}
