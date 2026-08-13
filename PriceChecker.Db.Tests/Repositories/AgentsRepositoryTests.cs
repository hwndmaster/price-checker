using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto.RequestMessages;

namespace Genius.PriceChecker.Db.Tests.Repositories;

public sealed class AgentsRepositoryTests
{
    [Fact]
    public async Task FullCrudLifecycle_GivenAgent_WhenCreatedUpdatedAndDeleted_ThenAllStagesSucceed()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var dateTime = new FakeDateTime();
        var repository = new AgentsRepository(dateTime, context);

        // Act & Assert: Create
        var created = await repository.CreateAsync(
            new CreateAgentRequest("amazon.de", "https://amazon.de/{0}", "pattern", "SimpleRegex", "."),
            cancellationToken: TestContext.Current.CancellationToken);
        var fetched = await repository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Equal("amazon.de", fetched.Key);
        Assert.Equal(".", fetched.DecimalDelimiter);

        // Act & Assert: Update
        dateTime.Advance(TimeSpan.FromMinutes(5));
        var updated = await repository.UpdateAsync(
            new UpdateAgentRequest(created.EntityId, created.LastModified, "amazon.nl", "https://amazon.nl/{0}", "pattern2", "SimpleRegexDivideBy100", ","),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEqual(created.LastModified, updated.LastModified);
        fetched = await repository.GetByIdOrThrowAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Equal("amazon.nl", fetched.Key);
        Assert.Equal(",", fetched.DecimalDelimiter);

        // Act & Assert: Delete
        await repository.DeleteAsync(created.EntityId, TestContext.Current.CancellationToken);
        Assert.Empty(await repository.GetAllAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExistsWithKeyAsync_GivenExistingKey_WhenCheckedWithAndWithoutExclusion_ThenRespondsAccordingly()
    {
        // Arrange
        await using var context = new RepositoryTestContext();
        var repository = new AgentsRepository(new FakeDateTime(), context);
        var created = await repository.CreateAsync(
            new CreateAgentRequest("bol.com", "https://bol.com/{0}", "pattern", "SimpleRegex", ","),
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        Assert.True(await repository.ExistsWithKeyAsync("bol.com", cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(await repository.ExistsWithKeyAsync("bol.com", created.EntityId, TestContext.Current.CancellationToken));
        Assert.False(await repository.ExistsWithKeyAsync("unknown", cancellationToken: TestContext.Current.CancellationToken));
    }
}
