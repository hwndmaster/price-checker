using System.Threading.Tasks;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.Core.CommandHandlers;

internal sealed class AgentsStoreWithOverwriteCommandHandler : ICommandHandler<AgentsStoreWithOverwriteCommand>
{
    private readonly IAgentRepository _agentRepo;
    private readonly IAgentQueryService _agentQuery;
    private readonly IProductRepository _productRepo;
    private readonly IProductQueryService _productQuery;
    private readonly IEventBus _eventBus;

    public AgentsStoreWithOverwriteCommandHandler(IAgentRepository agentRepo,
        IAgentQueryService agentQuery, IProductRepository productRepo,
        IProductQueryService productQuery, IEventBus eventBus)
    {
        _agentRepo = agentRepo;
        _eventBus = eventBus;
        _agentQuery = agentQuery;
        _productRepo = productRepo;
        _productQuery = productQuery;
    }

    public async Task ProcessAsync(AgentsStoreWithOverwriteCommand command)
    {
        _agentRepo.Overwrite(command.Agents);
        _eventBus.Publish(new AgentsAffectedEvent());

        await RefineProductSources();
    }

    private async Task RefineProductSources()
    {
        var agents = await _agentQuery.GetAllAsync();
        var agentsDict = agents.ToDictionary(x => x.Id);

        var products = (await _productQuery.GetAllAsync()).ToArray();
        HashSet<Guid> affectedProductsIds = new();
        foreach (var product in products)
        {
            var sources = product.Sources.ToList();
            foreach (var source in sources.ToList())
            {
                if (agentsDict.TryGetValue(source.Agent.Id, out var agent))
                {
                    if (!source.AgentKey.Equals(agent.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        source.AgentKey = agent.Key;
                        affectedProductsIds.Add(product.Id);
                    }
                }
                else
                {
                    sources.Remove(source);
                    affectedProductsIds.Add(product.Id);
                }
            }
            product.Sources = sources.ToArray();
        }

        if (!affectedProductsIds.Any())
            return;

        _productRepo.Overwrite(products);
        _eventBus.Publish(new EntitiesUpdatedEvent(affectedProductsIds));
    }
}
