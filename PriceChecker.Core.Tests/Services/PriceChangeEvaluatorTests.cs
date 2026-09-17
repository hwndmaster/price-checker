using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class PriceChangeEvaluatorTests
{
    private readonly PriceChangeEvaluator _sut = new();

    [Fact]
    public void Evaluate_GivenFirstScanEver_ThenNothingIsReported()
    {
        // Act
        var change = _sut.Evaluate(null, Overview(lowest: 100m, recent: 100m));

        // Assert: the first scan establishes a lowest price rather than beating one.
        Assert.False(change.HasNewLowestPrice);
        Assert.False(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenLowerPriceAndNoTarget_ThenNewLowestIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 100m, recent: 100m),
            Overview(lowest: 89.95m, recent: 89.95m));

        // Assert
        Assert.True(change.HasNewLowestPrice);
        Assert.True(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenHigherPriceAndNoTarget_ThenNothingIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 100m, recent: 100m),
            Overview(lowest: 100m, recent: 120m));

        // Assert
        Assert.False(change.HasNewLowestPrice);
        Assert.False(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenPriceCrossingTheTarget_ThenTargetIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 260m, recent: 289m, target: 250m),
            Overview(lowest: 249m, recent: 249m, target: 250m));

        // Assert
        Assert.True(change.HasReachedTarget);
        Assert.True(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenPriceStayingBelowTheTarget_ThenNothingIsReported()
    {
        // Act
        // A new lowest price all the same, so the products list still marks it; only the outbound
        // report is governed by the target, which was already reached by the previous scan.
        var change = _sut.Evaluate(
            Overview(lowest: 249m, recent: 249m, target: 250m),
            Overview(lowest: 245m, recent: 245m, target: 250m));

        // Assert
        Assert.True(change.HasNewLowestPrice);
        Assert.False(change.HasReachedTarget);
        Assert.False(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenNewLowestStillAboveTheTarget_ThenNothingIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 300m, recent: 300m, target: 250m),
            Overview(lowest: 280m, recent: 280m, target: 250m));

        // Assert: a product tracking a target asked about that target, not about its own history.
        Assert.True(change.HasNewLowestPrice);
        Assert.False(change.HasReachedTarget);
        Assert.False(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenPriceExactlyAtTheTarget_ThenTargetIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 260m, recent: 260m, target: 250m),
            Overview(lowest: 250m, recent: 250m, target: 250m));

        // Assert
        Assert.True(change.HasReachedTarget);
    }

    [Fact]
    public void Evaluate_GivenFirstScanBelowTheTarget_ThenTargetIsReported()
    {
        // Act
        var change = _sut.Evaluate(null, Overview(lowest: 199m, recent: 199m, target: 250m));

        // Assert: nothing was known before, so the target has been reached as far as the user is concerned.
        Assert.True(change.HasReachedTarget);
        Assert.True(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenTargetAndNoPriceFound_ThenNothingIsReported()
    {
        // Act
        var change = _sut.Evaluate(
            Overview(lowest: 300m, recent: 300m, target: 250m),
            Overview(lowest: 300m, recent: null, target: 250m));

        // Assert
        Assert.False(change.HasReachedTarget);
        Assert.False(change.IsWorthReporting);
    }

    [Fact]
    public void Evaluate_GivenLowestPriceOlderThanTheTarget_ThenTheRecentPriceDecides()
    {
        // Act
        // The product once cost 200 but is back up at 289: the all-time lowest is below the target
        // while what can actually be paid today is not.
        var change = _sut.Evaluate(
            Overview(lowest: 200m, recent: 300m, target: 250m),
            Overview(lowest: 200m, recent: 289m, target: 250m));

        // Assert
        Assert.False(change.HasReachedTarget);
        Assert.False(change.IsWorthReporting);
    }

    private static ProductOverviewDto Overview(decimal? lowest, decimal? recent, decimal? target = null)
        => new(
            new ProductRef(Guid.NewGuid()),
            "Test Product",
            Category: null,
            Description: null,
            Sources: [],
            ProductScanStatus.ScannedOk,
            StatusText: null,
            LowestPrice: lowest,
            LowestFoundDate: lowest is null ? null : DateTimeOffset.UnixEpoch,
            RecentPrice: recent,
            TargetPrice: target,
            LastScannedDate: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch);
}
