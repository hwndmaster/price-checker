using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class ProductStatusProviderTests
{
    private static readonly TimeSpan WinterOffset = TimeSpan.FromHours(1);

    private readonly FakeDateTime _dateTime = new();
    private readonly IScanSchedule _scanSchedule = new ScanSchedule(new ScanScheduleOptions
    {
        TimeOfDay = new TimeOnly(9, 0),
        TimeZone = "Europe/Amsterdam",
        OutdatedGrace = TimeSpan.FromHours(3),
    });

    [Fact]
    public void DetermineStatus_GivenNoPrices_ThenNotScanned()
    {
        // Act
        var status = CreateSut(new DateTimeOffset(2026, 1, 15, 12, 0, 0, WinterOffset)).DetermineStatus([]);

        // Assert
        Assert.Equal(ProductScanStatus.NotScanned, status);
    }

    [Fact]
    public void DetermineStatus_GivenPricesFromTodaysScan_WhenCheckedLater_ThenScannedOk()
    {
        // Arrange
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var prices = new[] { Snapshot(new DateTimeOffset(2026, 1, 15, 9, 5, 0, WinterOffset)) };

        // Act & Assert
        Assert.Equal(ProductScanStatus.ScannedOk, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DetermineStatus_GivenYesterdaysPrices_WhenBeforeTodaysTrigger_ThenNotYetOutdated()
    {
        // Arrange
        // 08:00 — today's trigger has not happened yet, so yesterday's prices are still current.
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 8, 0, 0, WinterOffset));
        var prices = new[] { Snapshot(new DateTimeOffset(2026, 1, 14, 9, 5, 0, WinterOffset)) };

        // Act & Assert
        Assert.Equal(ProductScanStatus.ScannedOk, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DetermineStatus_GivenYesterdaysPrices_WhenScanIsStillWithinTheGracePeriod_ThenNotYetOutdated()
    {
        // Arrange
        // 10:00 — today's scan is due but may still be running, so the grace period keeps the status.
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 10, 0, 0, WinterOffset));
        var prices = new[] { Snapshot(new DateTimeOffset(2026, 1, 14, 9, 5, 0, WinterOffset)) };

        // Act & Assert
        Assert.Equal(ProductScanStatus.ScannedOk, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DetermineStatus_GivenYesterdaysPrices_WhenGracePeriodElapsed_ThenOutdated()
    {
        // Arrange
        // 12:01 — three hours past the 09:00 trigger, so the scan genuinely did not refresh this.
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 12, 1, 0, WinterOffset));
        var prices = new[] { Snapshot(new DateTimeOffset(2026, 1, 14, 9, 5, 0, WinterOffset)) };

        // Act & Assert
        Assert.Equal(ProductScanStatus.Outdated, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DetermineStatus_GivenFreshPricesWithAFailure_ThenScannedWithErrors()
    {
        // Arrange
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var foundDate = new DateTimeOffset(2026, 1, 15, 9, 5, 0, WinterOffset);
        var prices = new[]
        {
            Snapshot(foundDate),
            Snapshot(foundDate, AgentHandlingStatus.CouldNotMatch),
        };

        // Act & Assert
        Assert.Equal(ProductScanStatus.ScannedWithErrors, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DetermineStatus_GivenStalePricesWithAFailure_ThenOutdatedTakesPrecedence()
    {
        // Arrange
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var foundDate = new DateTimeOffset(2026, 1, 10, 9, 5, 0, WinterOffset);
        var prices = new[]
        {
            Snapshot(foundDate),
            Snapshot(foundDate, AgentHandlingStatus.CouldNotMatch),
        };

        // Act & Assert
        Assert.Equal(ProductScanStatus.Outdated, sut.DetermineStatus(prices));
    }

    [Fact]
    public void DescribeIssues_GivenAllSourcesSucceeded_ThenNoDescription()
    {
        // Arrange
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var prices = new[] { Snapshot(new DateTimeOffset(2026, 1, 15, 9, 5, 0, WinterOffset)) };

        // Act & Assert
        Assert.Null(sut.DescribeIssues(prices));
    }

    [Fact]
    public void DescribeIssues_GivenFailingSources_ThenEachFailureIsNamedByItsAgent()
    {
        // Arrange
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var foundDate = new DateTimeOffset(2026, 1, 15, 9, 5, 0, WinterOffset);
        var prices = new[]
        {
            Snapshot(foundDate, agentKey: "ok-agent"),
            Snapshot(foundDate, AgentHandlingStatus.CouldNotMatch, "matchless-agent"),
            Snapshot(foundDate, AgentHandlingStatus.CouldNotFetch, "offline-agent"),
            // A second source running through the same agent, failing the same way.
            Snapshot(foundDate, AgentHandlingStatus.CouldNotMatch, "matchless-agent"),
        };

        // Act
        var description = sut.DescribeIssues(prices);

        // Assert
        Assert.NotNull(description);
        var lines = description.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("matchless-agent: ", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("offline-agent: ", lines[1], StringComparison.Ordinal);
        Assert.DoesNotContain("ok-agent", description, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeIssues_GivenStalePricesWithAFailure_ThenStillDescribesTheFailure()
    {
        // Arrange
        // The status reads as Outdated, but the failing source is what the user can act upon.
        var sut = CreateSut(new DateTimeOffset(2026, 1, 15, 18, 0, 0, WinterOffset));
        var foundDate = new DateTimeOffset(2026, 1, 10, 9, 5, 0, WinterOffset);
        var prices = new[] { Snapshot(foundDate, AgentHandlingStatus.InvalidPrice, "broken-agent") };

        // Act & Assert
        Assert.StartsWith("broken-agent: ", sut.DescribeIssues(prices), StringComparison.Ordinal);
    }

    private static PriceSnapshot Snapshot(DateTimeOffset foundDate,
        AgentHandlingStatus status = AgentHandlingStatus.Success, string agentKey = "test-agent")
        => new(status, agentKey, 100m, foundDate);

    private IProductStatusProvider CreateSut(DateTimeOffset now)
    {
        // FakeDateTime exposes NowUtc as the clock converted to UTC, so a UTC-kind clock round-trips exactly.
        _dateTime.SetClock(now.UtcDateTime);
        return new ProductStatusProvider(_dateTime, _scanSchedule);
    }
}
