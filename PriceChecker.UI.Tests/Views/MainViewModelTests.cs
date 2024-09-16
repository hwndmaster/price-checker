using System.Reactive.Subjects;
using System.Windows.Shell;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.TestingUtil;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.Views;

namespace Genius.PriceChecker.UI.Tests.Views;

public class MainViewModelTests
{
    private readonly Fixture _fixture = new();
    private readonly FakeTab<ITrackerViewModel> _fakeTrackerTab = new();
    private readonly FakeTab<IAgentsViewModel> _fakeAgentsTab = new();
    private readonly FakeTab<ISettingsViewModel> _fakeSettingsTab = new();
    private readonly FakeTab<ILogsTabViewModel> _fakeLogsTab = new();
    private readonly ITrackerScanContext _fakeScanContext = A.Fake<ITrackerScanContext>();
    private readonly INotifyIconViewModel _fakeNotifyViewModel = A.Fake<INotifyIconViewModel>();

    private readonly MainViewModel _sut;

    // Session values:
    private readonly Subject<(TrackerScanStatus Status, double Progress)> _scanProgressSubject = new();

    public MainViewModelTests()
    {
        A.CallTo(() => _fakeScanContext.ScanProgress).Returns(_scanProgressSubject);

        _sut = new(_fakeTrackerTab.Instance, _fakeAgentsTab.Instance, _fakeSettingsTab.Instance,
            _fakeLogsTab.Instance, _fakeScanContext, _fakeNotifyViewModel);
    }

    [Fact]
    public void Constructor__Tabs_are_populated()
    {
        // Verify
        Assert.Equal(4, _sut.Tabs.Count);
        Assert.Equal(_fakeTrackerTab.Instance, _sut.Tabs[0]);
        Assert.Equal(_fakeAgentsTab.Instance, _sut.Tabs[1]);
        Assert.Equal(_fakeSettingsTab.Instance, _sut.Tabs[2]);
        Assert.Equal(_fakeLogsTab.Instance, _sut.Tabs[3]);
    }

    [Fact]
    public void SelectedTabIndex_changed__Tab_is_Activated_and_old_deactivated()
    {
        // Arrange
        _sut.SelectedTabIndex = 0;
        _fakeTrackerTab.DropHistory();
        _fakeAgentsTab.DropHistory();
        _fakeSettingsTab.DropHistory();
        _fakeLogsTab.DropHistory();

        // Act
        const int settingsTabIndex = 2;
        _sut.SelectedTabIndex = settingsTabIndex;

        // Verify
        Assert.True(_fakeTrackerTab.OnlyOneDeactivated);
        Assert.Equal(0, _fakeAgentsTab.ActivatedCalls + _fakeAgentsTab.DeactivatedCalls);
        Assert.True(_fakeSettingsTab.OnlyOneActivated);
        Assert.Equal(0, _fakeLogsTab.ActivatedCalls + _fakeLogsTab.DeactivatedCalls);
    }

    [Fact]
    public void ScanProgress_changed__InProgress__Progress_state_highlighted_green()
    {
        // Arrange
        _sut.ProgressState = TaskbarItemProgressState.None;
        var progress = _fixture.Create<double>();

        // Act
        _scanProgressSubject.OnNext((TrackerScanStatus.InProgress, progress));

        // Verify
        Assert.Equal(TaskbarItemProgressState.Normal, _sut.ProgressState);
        Assert.Equal(progress, _sut.ProgressValue);
    }

    [Fact]
    public void ScanProgress_changed__InProgressWithErrors__Progress_state_highlighted_yellow()
    {
        // Arrange
        _sut.ProgressState = TaskbarItemProgressState.None;
        var progress = _fixture.Create<double>();

        // Act
        _scanProgressSubject.OnNext((TrackerScanStatus.InProgressWithErrors, progress));

        // Verify
        Assert.Equal(TaskbarItemProgressState.Paused, _sut.ProgressState);
        Assert.Equal(progress, _sut.ProgressValue);
    }

    [Fact]
    public void ScanProgress_changed__Finished__Progress_state_dropped_and_message_shown()
    {
        // Arrange
        _sut.ProgressState = TaskbarItemProgressState.Normal;
        _sut.ProgressValue = _fixture.Create<double>();

        // Act
        _scanProgressSubject.OnNext((TrackerScanStatus.Finished, _fixture.Create<double>()));

        // Verify
        Assert.Equal(TaskbarItemProgressState.None, _sut.ProgressState);
        Assert.Equal(0, _sut.ProgressValue);
    }
}

internal class FakeTab<T>
    where T: class, ITabViewModel
{
    private readonly T _fakeTab;
    public int ActivatedCalls;
    public int DeactivatedCalls;

    public FakeTab()
    {
        _fakeTab = A.Fake<T>();

        var activatedCommandFake = A.Fake<IActionCommand>();
        A.CallTo(() => activatedCommandFake.Execute(null)).Invokes((object _) => ActivatedCalls++);
        A.CallTo(() => _fakeTab.Activated).Returns(activatedCommandFake);

        var deactivatedCommandFake = A.Fake<IActionCommand>();
        A.CallTo(() => deactivatedCommandFake.Execute(null)).Invokes((object _) => DeactivatedCalls++);
        A.CallTo(() => _fakeTab.Deactivated).Returns(deactivatedCommandFake);
    }

    public void DropHistory()
    {
        ActivatedCalls = 0;
        DeactivatedCalls = 0;
    }

    public T Instance => _fakeTab;

    public bool OnlyOneActivated => ActivatedCalls == 1 && DeactivatedCalls == 0;
    public bool OnlyOneDeactivated => ActivatedCalls == 0 && DeactivatedCalls == 1;
}
