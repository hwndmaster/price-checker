using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.CommandHandlers;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.Tests.CommandHandlers;

public class AgentsStoreWithOverwriteCommandHandlerTests
{
    private readonly AgentsStoreWithOverwriteCommandHandler _sut;
    private readonly Fixture _fixture = new();
    private readonly IAgentRepository _fakeAgentRepo = A.Fake<IAgentRepository>();
    private readonly IAgentQueryService _fakeAgentQuery = A.Fake<IAgentQueryService>();
    private readonly IProductRepository _fakeProductRepo = A.Fake<IProductRepository>();
    private readonly IProductQueryService _fakeProductQuery = A.Fake<IProductQueryService>();
    private readonly IEventBus _fakeEventBus = A.Fake<IEventBus>();

    public AgentsStoreWithOverwriteCommandHandlerTests()
    {
        _sut = new(_fakeAgentRepo, _fakeAgentQuery, _fakeProductRepo, _fakeProductQuery, _fakeEventBus);
    }

    [Fact]
    public async Task Process__Agents_are_overwritten_and_event_published()
    {
        // Arrange
        var command = _fixture.Create<AgentsStoreWithOverwriteCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _fakeAgentRepo.OverwriteAsync(A<Agent[]>.That.IsSameSequenceAs(command.Agents))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _fakeEventBus.Publish(A<AgentsAffectedEvent>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Process__Products_agent_keys_are_refined()
    {
        // Arrange
        var products = ModelHelpers.SampleManyProducts().ToArray();
        var agents = ModelHelpers.SampleManyAgents(products).ToArray();
        A.CallTo(() => _fakeProductQuery.GetAllAsync()).Returns(products);

        var agentsToUpdate = ModelHelpers.Clone(agents);
        // Rename agent keys for #1 and #4:
        agentsToUpdate[1].Key = _fixture.Create<string>();
        agentsToUpdate[4].Key = _fixture.Create<string>();
        // Remove one agent:
        agentsToUpdate = agentsToUpdate.Except(new [] { agentsToUpdate[5] }).ToArray();
        var command = new AgentsStoreWithOverwriteCommand(agentsToUpdate);
        var affectedProductIds = new HashSet<Guid>
        {
            products[0].Id, // only renaming
            products[1].Id  // renaming and removing
        };
        Agent[] agentsUpdated = Array.Empty<Agent>();
        A.CallTo(() => _fakeAgentRepo.OverwriteAsync(A<Agent[]>.Ignored)).Invokes((Agent[] x) => agentsUpdated = x);
        A.CallTo(() => _fakeAgentQuery.GetAllAsync()).ReturnsLazily(() => agentsUpdated);

        // Pre-Assert
        Assert.Equal(agents[1].Key, products[0].Sources[1].AgentKey);
        Assert.Equal(agents[4].Key, products[1].Sources[1].AgentKey);
        Assert.Equal(3, products[1].Sources.Length);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        A.CallTo(() => _fakeProductRepo.OverwriteAsync(A<Product[]>.That.Matches(x =>
            x[0].Sources[1].AgentKey == agentsToUpdate[1].Key
            && x[1].Sources[1].AgentKey == agentsToUpdate[4].Key
            && x[1].Sources.Length == 2
        ))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _fakeEventBus.Publish(A<EntitiesAffectedEvent>.That.Matches(x =>
            x.Updated.Keys.SequenceEqual(affectedProductIds)
        ))).MustHaveHappenedOnceExactly();
    }
}
