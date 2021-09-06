using System.Reactive.Subjects;
using System.Windows.Shell;
using AutoFixture;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.TestingUtil;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ViewModels;
using Moq;
using Xunit;

namespace Genius.PriceChecker.UI.Tests.ViewModels
{
    public class MainViewModelTests : TestBase
    {
        private readonly TabMock<ITrackerViewModel> _trackerMock = new();
        private readonly TabMock<IAgentsViewModel> _agentsMock = new();
        private readonly TabMock<ISettingsViewModel> _settingsMock = new();
        private readonly TabMock<ILogsViewModel> _logsMock = new();
        private readonly Mock<ITrackerScanContext> _scanContextMock = new();
        private readonly Mock<INotifyIconViewModel> _notifyViewModelMock = new();

        private readonly MainViewModel _sut;

        // Session values:
        private readonly Subject<(TrackerScanStatus Status, double Progress)> _scanProgressSubject = new();

        public MainViewModelTests()
        {
            _scanContextMock.SetupGet(x => x.ScanProgress).Returns(_scanProgressSubject);

            _sut = new(_trackerMock.Object, _agentsMock.Object, _settingsMock.Object,
                _logsMock.Object, _scanContextMock.Object, _notifyViewModelMock.Object);
        }

        [Fact]
        public void Constructor__Tabs_are_populated()
        {
            // Verify
            Assert.Equal(4, _sut.Tabs.Count);
            Assert.Equal(_trackerMock.Object, _sut.Tabs[0]);
            Assert.Equal(_agentsMock.Object, _sut.Tabs[1]);
            Assert.Equal(_settingsMock.Object, _sut.Tabs[2]);
            Assert.Equal(_logsMock.Object, _sut.Tabs[3]);
        }

        [Fact]
        public void SelectedTabIndex_changed__Tab_is_Activated_and_old_deactivated()
        {
            // Arrange
            _sut.SelectedTabIndex = 0;
            _trackerMock.DropHistory();
            _agentsMock.DropHistory();
            _settingsMock.DropHistory();
            _logsMock.DropHistory();

            // Act
            const int settingsTabIndex = 2;
            _sut.SelectedTabIndex = settingsTabIndex;

            // Verify
            Assert.True(_trackerMock.OnlyOneDeactivated);
            Assert.Equal(0, _agentsMock.ActivatedCalls + _agentsMock.DeactivatedCalls);
            Assert.True(_settingsMock.OnlyOneActivated);
            Assert.Equal(0, _logsMock.ActivatedCalls + _logsMock.DeactivatedCalls);
        }

        [Fact]
        public void ScanProgress_changed__InProgress__Progress_state_highlighted_green()
        {
            // Arrange
            _sut.ProgressState = TaskbarItemProgressState.None;
            var progress = Fixture.Create<double>();

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
            var progress = Fixture.Create<double>();

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
            _sut.ProgressValue = Fixture.Create<double>();

            // Act
            _scanProgressSubject.OnNext((TrackerScanStatus.Finished, Fixture.Create<double>()));

            // Verify
            Assert.Equal(TaskbarItemProgressState.None, _sut.ProgressState);
            Assert.Equal(0, _sut.ProgressValue);
        }
    }

    internal class TabMock<T> : Mock<T>
        where T: class, ITabViewModel
    {
        public int ActivatedCalls = 0;
        public int DeactivatedCalls = 0;

        public TabMock()
        {
            var activatedCommandMock = new Mock<IActionCommand>();
            activatedCommandMock.Setup(x => x.Execute(null)).Callback((object _) => ActivatedCalls++);
            SetupGet(x => x.Activated).Returns(activatedCommandMock.Object);

            var deactivatedCommandMock = new Mock<IActionCommand>();
            deactivatedCommandMock.Setup(x => x.Execute(null)).Callback((object _) => DeactivatedCalls++);
            SetupGet(x => x.Deactivated).Returns(deactivatedCommandMock.Object);
        }

        public void DropHistory()
        {
            ActivatedCalls = 0;
            DeactivatedCalls = 0;
        }

        public bool OnlyOneActivated => ActivatedCalls == 1 && DeactivatedCalls == 0;
        public bool OnlyOneDeactivated => ActivatedCalls == 0 && DeactivatedCalls == 1;
    }
}
