using System;
using System.Reactive.Subjects;
using AutoFixture;
using Genius.Atom.UI.Forms.TestingUtil;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Helpers;
using Xunit;

namespace Genius.PriceChecker.UI.Tests.Helpers;

public class TrackerScanContextTests : TestBase
{
    private readonly TrackerScanContext _sut;

    // Session values:
    private readonly Subject<ProductAutoScanStartedEvent> _productAutoScanStartedEventSubject;
    private TrackerScanStatus? _lastStatus = null;
    private double? _lastProgress = null;

    public TrackerScanContextTests()
    {
        _productAutoScanStartedEventSubject = CreateEventSubject<ProductAutoScanStartedEvent>();

        _sut = new TrackerScanContext(EventBusMock.Object);

        _sut.ScanProgress.Subscribe(x => {
            _lastStatus = x.Status;
            _lastProgress = x.Progress;
        });
    }

    [Fact]
    public void NotifyStarted__Resets_state_and_calculates_initial_progress()
    {
        // Arrange
        const int count = 10;

        // Act
        _sut.NotifyStarted(count);

        // Verify
        const double expectedProgress = 1d / (count * 2);
        Assert.True(_sut.IsStarted);
        Assert.False(_sut.HasErrors);
        Assert.False(_sut.HasNewLowestPrice);
        Assert.Equal(0, _sut.FinishedJobs);
        Assert.Equal(TrackerScanStatus.InProgress, _lastStatus);
        Assert.Equal(expectedProgress, _lastProgress);
    }

    [Fact]
    public void NotifyProgressChange__When_ScannedOk__Increases_progress()
    {
        // Arrange
        const int count = 2;
        _sut.NotifyStarted(count);

        // Act
        _sut.NotifyProgressChange(ProductScanStatus.ScannedOk);

        // Verify
        const double expectedProgress = 0.5d; // 50% of 2 jobs
        Assert.True(_sut.IsStarted);
        Assert.False(_sut.HasErrors);
        Assert.False(_sut.HasNewLowestPrice);
        Assert.Equal(1, _sut.FinishedJobs);
        Assert.Equal(TrackerScanStatus.InProgress, _lastStatus);
        Assert.Equal(expectedProgress, _lastProgress);
    }

    [Fact]
    public void NotifyProgressChange__When_scanned_last_job__Finishes_progress()
    {
        // Arrange
        _sut.NotifyStarted(2);

        // Act
        _sut.NotifyProgressChange(ProductScanStatus.ScannedOk);
        _sut.NotifyProgressChange(ProductScanStatus.ScannedOk);

        // Verify
        Assert.False(_sut.IsStarted);
        Assert.False(_sut.HasErrors);
        Assert.False(_sut.HasNewLowestPrice);
        Assert.Equal(2, _sut.FinishedJobs);
        Assert.Equal(TrackerScanStatus.Finished, _lastStatus);
        Assert.Equal(1, _lastProgress);
    }

    [Fact]
    public void NotifyProgressChange__When_ScannedWithErrors__Reports_about_errors()
    {
        // Arrange
        _sut.NotifyStarted(Fixture.Create<int>());

        // Act
        _sut.NotifyProgressChange(ProductScanStatus.ScannedWithErrors);

        // Verify
        Assert.True(_sut.HasErrors);
        Assert.Equal(TrackerScanStatus.InProgressWithErrors, _lastStatus);
    }

    [Fact]
    public void NotifyProgressChange__When_ScannedNewLowest__Reports_about_new_lowest()
    {
        // Arrange
        _sut.NotifyStarted(Fixture.Create<int>());

        // Act
        _sut.NotifyProgressChange(ProductScanStatus.ScannedNewLowest);

        // Verify
        Assert.True(_sut.HasNewLowestPrice);
    }

    [Fact]
    public void ProductAutoScanStartedEvent_fired__Calls_NotifyStarted()
    {
        // Arrange
        const int count = 10;

        // Act
        _productAutoScanStartedEventSubject.OnNext(new ProductAutoScanStartedEvent(count));

        // Verify
        const double expectedProgress = 1d / (count * 2);
        Assert.True(_sut.IsStarted);
        Assert.False(_sut.HasErrors);
        Assert.False(_sut.HasNewLowestPrice);
        Assert.Equal(0, _sut.FinishedJobs);
        Assert.Equal(TrackerScanStatus.InProgress, _lastStatus);
        Assert.Equal(expectedProgress, _lastProgress);
    }
}
