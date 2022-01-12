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
    private readonly Mock<IAgentRepository> _agentRepoMock = new();
    private readonly Mock<IAgentQueryService> _agentQueryMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IProductQueryService> _productQueryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();

    public AgentsStoreWithOverwriteCommandHandlerTests()
    {
        _sut = new(_agentRepoMock.Object, _agentQueryMock.Object, _productRepoMock.Object,
            _productQueryMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Process__Agents_are_overwritten_and_event_published()
    {
        // Arrange
        var command = _fixture.Create<AgentsStoreWithOverwriteCommand>();

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        _agentRepoMock.Verify(x => x.Overwrite(It.IsAny<Agent[]>()), Times.Once);
        _eventBusMock.Verify(x => x.Publish(It.IsAny<AgentsAffectedEvent>()), Times.Once);
    }

    [Fact]
    public async Task Process__Products_agent_keys_are_refined()
    {
        // Arrange
        var products = ModelHelpers.SampleManyProducts().ToArray();
        var agents = ModelHelpers.SampleManyAgents(products).ToArray();
        _productQueryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(products);

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
        _agentRepoMock.Setup(x => x.Overwrite(It.IsAny<Agent[]>()))
            .Callback<Agent[]>(x => agentsUpdated = x);
        _agentQueryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(agentsUpdated);

        // Pre-Assert
        Assert.Equal(agents[1].Key, products[0].Sources[1].AgentKey);
        Assert.Equal(agents[4].Key, products[1].Sources[1].AgentKey);
        Assert.Equal(3, products[1].Sources.Length);

        // Act
        await _sut.ProcessAsync(command);

        // Verify
        _productRepoMock.Verify(x => x.Overwrite(It.Is<Product[]>(y =>
            y[0].Sources[1].AgentKey.Equals(agentsToUpdate[1].Key)
            && y[1].Sources[1].AgentKey.Equals(agentsToUpdate[4].Key)
            && y[1].Sources.Length == 2
            )), Times.Once);
        _eventBusMock.Verify(x => x.Publish(It.Is<EntitiesUpdatedEvent>(y =>
            y.Entities.SequenceEqual(affectedProductIds))), Times.Once);
    }
}
