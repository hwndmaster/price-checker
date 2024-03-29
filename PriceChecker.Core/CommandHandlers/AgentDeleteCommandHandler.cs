using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class AgentDeleteCommandHandler : ICommandHandler<AgentDeleteCommand>
{
    private readonly IAgentRepository _agentRepo;
    private readonly IEventBus _eventBus;

    public AgentDeleteCommandHandler(IAgentRepository agentRepo, IEventBus eventBus)
    {
        _agentRepo = agentRepo;
        _eventBus = eventBus;
    }

    public async Task ProcessAsync(AgentDeleteCommand command)
    {
        await _agentRepo.DeleteAsync(command.AgentId);

        _eventBus.Publish(new AgentsAffectedEvent());
    }
}
