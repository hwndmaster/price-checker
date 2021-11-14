using System.Linq;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Persistence;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories;

public interface IAgentQueryService : IEntityQueryService<Agent>
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

    public Agent? FindByKey(string agentKey)
    {
        return GetAll().FirstOrDefault(x => x.Key == agentKey);
    }
}
