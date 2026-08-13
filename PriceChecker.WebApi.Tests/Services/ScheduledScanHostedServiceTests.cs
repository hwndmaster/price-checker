using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.WebApi.Services;

namespace Genius.PriceChecker.WebApi.Tests.Services;

public sealed class ScheduledScanHostedServiceTests
{
    private static readonly DateTimeOffset Trigger = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectDueProducts_GivenNoProducts_ThenNothingToScan()
    {
        // Act
        var due = ScheduledScanHostedService.SelectDueProducts([], Trigger);

        // Assert
        Assert.Empty(due);
    }

    [Fact]
    public void SelectDueProducts_GivenAProductScannedAfterTheTrigger_ThenTheRunAlreadyHappened()
    {
        // Arrange
        var overviews = new[]
        {
            Overview(Trigger.AddMinutes(5)),
            Overview(Trigger.AddDays(-1)),
        };

        // Act
        var due = ScheduledScanHostedService.SelectDueProducts(overviews, Trigger);

        // Assert
        Assert.Empty(due);
    }

    [Fact]
    public void SelectDueProducts_GivenAProductScannedExactlyAtTheTrigger_ThenTheRunAlreadyHappened()
    {
        // Act
        var due = ScheduledScanHostedService.SelectDueProducts([Overview(Trigger)], Trigger);

        // Assert
        Assert.Empty(due);
    }

    [Fact]
    public void SelectDueProducts_GivenEverythingScannedBeforeTheTrigger_ThenAllAreDue()
    {
        // Arrange
        // This is the missed-run case: the app was down at the scheduled moment, so the scan
        // must happen as soon as it is back up.
        var overviews = new[]
        {
            Overview(Trigger.AddHours(-20)),
            Overview(Trigger.AddDays(-3)),
            Overview(lastScannedDate: null),
        };

        // Act
        var due = ScheduledScanHostedService.SelectDueProducts(overviews, Trigger);

        // Assert
        Assert.Equal(3, due.Length);
    }

    [Fact]
    public void SelectDueProducts_GivenOnlyNeverScannedProducts_ThenAllAreDue()
    {
        // Act
        var due = ScheduledScanHostedService.SelectDueProducts([Overview(lastScannedDate: null)], Trigger);

        // Assert
        Assert.Single(due);
    }

    private static ProductOverviewDto Overview(DateTimeOffset? lastScannedDate)
        => new(
            new ProductRef(Guid.NewGuid()),
            "Test Product",
            Category: null,
            Description: null,
            ProductScanStatus.ScannedOk,
            LowestPrice: null,
            LowestFoundDate: null,
            RecentPrice: null,
            lastScannedDate,
            LastModified: Trigger);
}
