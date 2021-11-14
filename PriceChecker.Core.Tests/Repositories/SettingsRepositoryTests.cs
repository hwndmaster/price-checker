using System;
using AutoFixture;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Persistence;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Genius.PriceChecker.Core.Tests.Repositories;

public class SettingsRepositoryTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<IJsonPersister> _persisterMock = new();

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
        _persisterMock.Verify(x => x.Store(It.IsAny<string>(), newSettings));
        _eventBusMock.Verify(x => x.Publish(It.Is<SettingsUpdatedEvent>(e => e.Settings == newSettings)), Times.Once);
    }

    private SettingsRepository CreateSystemUnderTest(Settings? settings = null)
    {
        _persisterMock.Setup(x => x.Load<Settings>(It.IsAny<string>()))
            .Returns(settings!);
        return new SettingsRepository(_eventBusMock.Object, _persisterMock.Object,
            Mock.Of<ILogger<SettingsRepository>>());
    }
}
