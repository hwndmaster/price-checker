using System;
using System.Collections.Generic;
using System.Linq;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories
{
    public interface IAgentRepository
    {
        IEnumerable<Agent> GetAll();
        Agent FindById(string id);
        void Delete(string agentId);
        void Store(ICollection<Agent> agents);
    }

    internal sealed class AgentRepository : IAgentRepository
    {
        private readonly IEventBus _eventBus;
        private readonly IPersister _persister;
        private readonly ILogger<AgentRepository> _logger;

        private const string FILENAME = @".\agents.json";
        private List<Agent> _agents = null;

        public AgentRepository(IEventBus eventBus, IPersister persister, ILogger<AgentRepository> logger)
        {
            _eventBus = eventBus;
            _persister = persister;
            _logger = logger;

            _agents = _persister.LoadCollection<Agent>(FILENAME).ToList();
        }

        public IEnumerable<Agent> GetAll()
        {
            return _agents.OrderBy(x => x.Id);
        }

        public Agent FindById(string agentId)
        {
            return GetAll().FirstOrDefault(x => x.Id == agentId);
        }

        public void Delete(string agentId)
        {
            var agent = _agents.FirstOrDefault(x => x.Id == agentId);
            if (agent == null)
            {
                _logger.LogWarning($"Cannot find agent '{agentId}' to delete");
                return;
            }

            _agents.Remove(agent);

            _persister.Store(FILENAME, _agents);
            _eventBus.Publish(new AgentDeletedEvent(agent));
        }

        public void Store(ICollection<Agent> agents)
        {
            if (agents.Any(x => string.IsNullOrEmpty(x.Id)))
            {
                throw new InvalidOperationException("Agent must be supplied with an indentifier.");
            }

            _agents = agents.ToList();
            _persister.Store(FILENAME, _agents);
            _eventBus.Publish(new AgentsUpdatedEvent());

            _logger.LogInformation($"Agents updated.");
        }
    }
}
