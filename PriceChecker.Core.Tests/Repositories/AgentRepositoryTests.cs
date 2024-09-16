using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.Tests.Repositories;

public class AgentRepositoryTests : IDisposable
{
    private readonly AgentRepository _sut;
    private readonly Fixture _fixture = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly IJsonPersister _persisterMock = A.Fake<IJsonPersister>();

    private readonly Agent[] _agents;

    public AgentRepositoryTests()
    {
        _agents = _fixture.CreateMany<Agent>().ToArray();

        A.CallTo(() => _persisterMock.LoadCollection<Agent>(A<string>._))
            .Returns(_agents);

        _sut = new AgentRepository(_eventBus, _persisterMock, new FakeLogger<AgentRepository>());

        _sut.GetAllAsync().GetAwaiter().GetResult(); // To trigger the initializer
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public async Task FindById__Returns_appropriate_agent()
    {
        // Arrange
        var agentToFind = _agents[1];

        // Act
        var result = await _sut.FindByIdAsync(agentToFind.Id);

        // Verify
        Assert.Equal(agentToFind, result);
    }

    [Fact]
    public async Task Delete__Removes_appripriate_agent()
    {
        // Arrange
        var agentToDelete = _agents[1];

        // Act
        await _sut.DeleteAsync(agentToDelete.Id);

        // Verify
        Assert.Null(await _sut.FindByIdAsync(agentToDelete.Id));
    }

    [Fact]
    public async Task Delete__When_no_agent_found__Breaks_operation()
    {
        // Arrange
        var agents = await _sut.GetAllAsync();
        var agentCount = agents.Count();

        // Act
        await _sut.DeleteAsync(Guid.NewGuid());

        // Verify
        Assert.Equal(agentCount, (await _sut.GetAllAsync()).Count());
    }

    [Fact]
    public async Task Store__Replaces_all_existing_agents_and_updates_cache_and_fires_event()
    {
        // Arrange
        var newAgents = _fixture.CreateMany<Agent>().ToArray();
        var previousAgents = (await _sut.GetAllAsync()).ToArray();

        // Act
        await _sut.OverwriteAsync(newAgents);

        // Verify
        Assert.False((await _sut.GetAllAsync()).Except(newAgents).Any());

        A.CallTo(() => _persisterMock.Store(A<string>._, A<IEnumerable<Agent>>.That.IsSameSequenceAs(newAgents)))
            .MustHaveHappenedOnceExactly();
        _eventBus.AssertSingleEvent<EntitiesAffectedEvent>(e => e.Added.Count == newAgents.Length
            && e.Deleted.Count == previousAgents.Length);
    }
}
