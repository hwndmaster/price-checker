using Genius.Atom.Data.IdHandlers;
using Genius.Atom.Data.JsonPersistence;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IAgentQueryService : IQueryService<Agent>
{
    Task<Agent?> FindByKeyAsync(string agentKey);
}

public interface IAgentRepository : IJsonRepository<Guid, AgentRef, Agent>
{
}

internal sealed class AgentRepository : JsonRepositoryBase<Guid, AgentRef, Agent>, IAgentRepository, IAgentQueryService
{
    public AgentRepository(IEventBus eventBus, IJsonPersister persister, IIdHandler<Guid> idHandler,
        ILogger<AgentRepository> logger)
        : base(eventBus, persister, idHandler, logger)
    {
    }

    public Task<Agent?> FindByIdAsync(Guid entityId)
        => base.FindByIdAsync(AgentRef.Create(entityId));

    public async Task<Agent?> FindByKeyAsync(string agentKey)
    {
        return (await GetAllAsync()).FirstOrDefault(x => x.Key == agentKey);
    }

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
