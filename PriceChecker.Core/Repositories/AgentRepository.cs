using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IAgentQueryService : IQueryService<Agent>
{
    Task<Agent?> FindByKeyAsync(string agentKey);
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

    public new Task<Agent?> FindByIdAsync(Guid entityId)
        => base.FindByIdAsync(entityId);

    public async Task<Agent?> FindByKeyAsync(string agentKey)
    {
        return (await GetAllAsync()).FirstOrDefault(x => x.Key == agentKey);
    }

    public new Task<IEnumerable<Agent>> GetAllAsync()
        => base.GetAllAsync();

    protected override Task FillUpRelationsAsync(Agent entity)
    {
        // Backwards compatibility
        if (string.IsNullOrEmpty(entity.Handler))
        {
            entity.Handler = nameof(AgentHandlers.SimpleRegex);
        }

        return Task.CompletedTask;
    }
}
