using Genius.PriceChecker.WebApi.Services;

namespace Genius.PriceChecker.WebApi.Tests.Services;

public sealed class ScanContextTests
{
    private readonly ScanContext _sut = new();

    [Fact]
    public void GetProgress_GivenNoScans_WhenFetched_ThenReportsFinished()
    {
        // Act
        var progress = _sut.GetProgress();

        // Assert
        Assert.True(progress.IsFinished);
        Assert.Equal(0, progress.Total);
    }

    [Fact]
    public void NotifyProductFinished_GivenRunningScan_WhenAllProductsFinish_ThenProgressCompletes()
    {
        // Arrange
        _sut.NotifyScanStarted(2);

        // Act & Assert: halfway
        _sut.NotifyProductFinished(hasErrors: false, hasNewLowestPrice: false);
        var progress = _sut.GetProgress();
        Assert.False(progress.IsFinished);
        Assert.Equal(1, progress.Finished);
        Assert.Equal(2, progress.Total);

        // Act & Assert: done, with flags aggregated
        _sut.NotifyProductFinished(hasErrors: true, hasNewLowestPrice: true);
        progress = _sut.GetProgress();
        Assert.True(progress.IsFinished);
        Assert.True(progress.HasErrors);
        Assert.True(progress.HasNewLowestPrice);
    }

    [Fact]
    public void NotifyScanStarted_GivenFinishedScan_WhenNewScanStarts_ThenCountersReset()
    {
        // Arrange
        _sut.NotifyScanStarted(1);
        _sut.NotifyProductFinished(hasErrors: true, hasNewLowestPrice: false);

        // Act
        _sut.NotifyScanStarted(3);

        // Assert
        var progress = _sut.GetProgress();
        Assert.Equal(0, progress.Finished);
        Assert.Equal(3, progress.Total);
        Assert.False(progress.HasErrors);
        Assert.False(progress.IsFinished);
    }

    [Fact]
    public void NotifyScanStarted_GivenRunningScan_WhenMoreProductsEnqueued_ThenTotalGrows()
    {
        // Arrange
        _sut.NotifyScanStarted(2);
        _sut.NotifyProductFinished(hasErrors: false, hasNewLowestPrice: false);

        // Act
        _sut.NotifyScanStarted(2);

        // Assert
        var progress = _sut.GetProgress();
        Assert.Equal(1, progress.Finished);
        Assert.Equal(4, progress.Total);
    }
}
