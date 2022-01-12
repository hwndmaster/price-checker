using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IAgentQueryService : IQueryService<Agent>
{
    Agent? FindByKey(string agentKey);
}

public interface IAgentRepository : IRepository<Agent>
{
}

internal sealed class AgentRepository : RepositoryBase<Agent>, IAgentRepository, IAgentQueryService
{
    public AgentRepository(IEventBus eventBus, IJsonPersister persister, ILogger<AgentRepository> logger)
        : base(eventBus, persister, logger)
    {
    }

    public Task<Agent?> FindByIdAsync(Guid entityId)
    {
        return Task.FromResult(base.FindById(entityId));
    }

    public Agent? FindByKey(string agentKey)
    {
        return GetAll().FirstOrDefault(x => x.Key == agentKey);
    }

    public Task<IEnumerable<Agent>> GetAllAsync()
    {
        return Task.FromResult(base.GetAll());
    }

    protected override void FillUpRelations(Agent entity)
    {
        // Backwards compatibility
        if (string.IsNullOrEmpty(entity.Handler))
        {
            entity.Handler = nameof(AgentHandlers.SimpleRegex);
        }

        base.FillUpRelations(entity);
    }
}
