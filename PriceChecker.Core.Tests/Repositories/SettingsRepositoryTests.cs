using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Repositories;

public class SettingsRepositoryTests
{
    private readonly Fixture _fixture = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly IJsonPersister _persisterMock = A.Fake<IJsonPersister>();

    [Fact]
    public void Constructor__Previous_settings_exist__Loaded()
    {
        // Arrange
        var settings = _fixture.Create<Settings>();

        // Act
        var sut = CreateSystemUnderTest(settings);

        // Verify
        Assert.Equal(settings, sut.Get());
    }

    [Fact]
    public void Constructor__Previous_settings_dont_exist__Loaded_default()
    {
        // Arrange
        Settings? settings = null;

        // Act
        var sut = CreateSystemUnderTest(settings);

        // Verify
        var result = sut.Get();
        Assert.False(result.AutoRefreshEnabled);
        Assert.Equal(1440, result.AutoRefreshMinutes);
    }

    [Fact]
    public void Get__returns_currently_loaded_settings()
    {
        // Arrange
        var settings = _fixture.Create<Settings>();
        var sut = CreateSystemUnderTest(settings);

        // Act
        var result = sut.Get();

        // Verify
        Assert.Equal(settings, result);
    }

    [Fact]
    public void Store__Argument_not_provided__throws_exception()
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Act & Verify
        Assert.Throws<ArgumentNullException>(() => sut.Store(null!));
    }

    [Fact]
    public void Store__Replaces_existing_settings_and_updates_cache_and_fires_event()
    {
        // Arrange
        var sut = CreateSystemUnderTest();
        var newSettings = _fixture.Create<Settings>();

        // Act
        sut.Store(newSettings);

        // Verify
        Assert.Equal(newSettings, sut.Get());
        A.CallTo(() => _persisterMock.Store(A<string>._, newSettings)).MustHaveHappenedOnceExactly();
        _eventBus.AssertSingleEvent<SettingsUpdatedEvent>(e => e.Settings == newSettings);
    }

    private SettingsRepository CreateSystemUnderTest(Settings? settings = null)
    {
        A.CallTo(() => _persisterMock.Load<Settings>(A<string>._)).Returns(settings);

        return new SettingsRepository(_eventBus, _persisterMock, new FakeLogger<SettingsRepository>());
    }
}
