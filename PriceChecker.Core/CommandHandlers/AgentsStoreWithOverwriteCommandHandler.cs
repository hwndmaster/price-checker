using System.Threading.Tasks;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers
{
    internal sealed class AgentsStoreWithOverwriteCommandHandler : ICommandHandler<AgentsStoreWithOverwriteCommand>
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IEventBus _eventBus;

        public AgentsStoreWithOverwriteCommandHandler(IAgentRepository agentRepo, IEventBus eventBus)
        {
            _agentRepo = agentRepo;
            _eventBus = eventBus;
        }

        public Task ProcessAsync(AgentsStoreWithOverwriteCommand command)
        {
            _agentRepo.Overwrite(command.Agents);

            _eventBus.Publish(new AgentsAffectedEvent());

            return Task.CompletedTask;
        }
    }
}
