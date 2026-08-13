using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class ScanScheduleTests
{
    private static readonly TimeSpan WinterOffset = TimeSpan.FromHours(1);  // CET
    private static readonly TimeSpan SummerOffset = TimeSpan.FromHours(2);  // CEST

    [Fact]
    public void GetMostRecentTrigger_GivenNowPastTodaysTime_WhenResolved_ThenReturnsToday()
    {
        // Arrange
        var sut = CreateSut();
        var now = new DateTimeOffset(2026, 1, 15, 9, 30, 0, WinterOffset);

        // Act
        var trigger = sut.GetMostRecentTrigger(now);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 9, 0, 0, WinterOffset), trigger);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenNowBeforeTodaysTime_WhenResolved_ThenReturnsYesterday()
    {
        // Arrange
        var sut = CreateSut();
        var now = new DateTimeOffset(2026, 1, 15, 8, 59, 0, WinterOffset);

        // Act
        var trigger = sut.GetMostRecentTrigger(now);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 1, 14, 9, 0, 0, WinterOffset), trigger);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenNowExactlyAtTheTime_WhenResolved_ThenReturnsToday()
    {
        // Arrange
        var sut = CreateSut();
        var now = new DateTimeOffset(2026, 1, 15, 9, 0, 0, WinterOffset);

        // Act
        var trigger = sut.GetMostRecentTrigger(now);

        // Assert
        Assert.Equal(now, trigger);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenUtcInputJustAfterLocalMidnight_WhenResolved_ThenUsesLocalDate()
    {
        // Arrange
        // 23:30 UTC on the 14th is already 00:30 local on the 15th, so the most recent trigger is
        // the 14th's — resolving against the UTC date would wrongly pick the 13th.
        var sut = CreateSut();
        var now = new DateTimeOffset(2026, 1, 14, 23, 30, 0, TimeSpan.Zero);

        // Act
        var trigger = sut.GetMostRecentTrigger(now);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 1, 14, 9, 0, 0, WinterOffset), trigger);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenDatesAcrossTheSpringTransition_WhenResolved_ThenStaysAtLocalNine()
    {
        // Arrange
        // The Dutch clocks jump forward on 2026-03-29. 09:00 must stay 09:00 wall-clock, which means
        // the absolute moment shifts by an hour (08:00 UTC before, 07:00 UTC after).
        var sut = CreateSut();

        // Act
        var before = sut.GetMostRecentTrigger(new DateTimeOffset(2026, 3, 28, 12, 0, 0, WinterOffset));
        var after = sut.GetMostRecentTrigger(new DateTimeOffset(2026, 3, 30, 12, 0, 0, SummerOffset));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 3, 28, 8, 0, 0, TimeSpan.Zero), before.ToUniversalTime());
        Assert.Equal(new DateTimeOffset(2026, 3, 30, 7, 0, 0, TimeSpan.Zero), after.ToUniversalTime());
        Assert.Equal(9, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(before, "Europe/Amsterdam").Hour);
        Assert.Equal(9, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(after, "Europe/Amsterdam").Hour);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenDatesAcrossTheAutumnTransition_WhenResolved_ThenStaysAtLocalNine()
    {
        // Arrange
        // The Dutch clocks jump back on 2026-10-25.
        var sut = CreateSut();

        // Act
        var before = sut.GetMostRecentTrigger(new DateTimeOffset(2026, 10, 24, 12, 0, 0, SummerOffset));
        var after = sut.GetMostRecentTrigger(new DateTimeOffset(2026, 10, 26, 12, 0, 0, WinterOffset));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 7, 0, 0, TimeSpan.Zero), before.ToUniversalTime());
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 8, 0, 0, TimeSpan.Zero), after.ToUniversalTime());
        Assert.Equal(9, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(before, "Europe/Amsterdam").Hour);
        Assert.Equal(9, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(after, "Europe/Amsterdam").Hour);
    }

    [Fact]
    public void GetMostRecentTrigger_GivenTheTimeFallsInTheSpringForwardGap_WhenResolved_ThenSkipsPastTheGap()
    {
        // Arrange
        // 02:30 does not exist on the day the clocks jump forward; the trigger must still land on that day.
        var sut = CreateSut(timeOfDay: new TimeOnly(2, 30));

        // Act
        var trigger = sut.GetMostRecentTrigger(new DateTimeOffset(2026, 3, 29, 12, 0, 0, SummerOffset));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero), trigger.ToUniversalTime());
    }

    [Fact]
    public void Options_GivenConfiguredValues_WhenExposed_ThenPassedThrough()
    {
        // Arrange
        var sut = CreateSut(enabled: false, outdatedGrace: TimeSpan.FromHours(5));

        // Act & Assert
        Assert.False(sut.Enabled);
        Assert.Equal(TimeSpan.FromHours(5), sut.OutdatedGrace);
    }

    private static IScanSchedule CreateSut(TimeOnly? timeOfDay = null, bool enabled = true,
        TimeSpan? outdatedGrace = null)
        => new ScanSchedule(new ScanScheduleOptions
        {
            Enabled = enabled,
            TimeOfDay = timeOfDay ?? new TimeOnly(9, 0),
            TimeZone = "Europe/Amsterdam",
            OutdatedGrace = outdatedGrace ?? TimeSpan.FromHours(3),
        });
}
