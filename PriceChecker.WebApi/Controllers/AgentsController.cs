using Genius.Atom.Data.Validation;
using Genius.Atom.Web.Controllers;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;
using Microsoft.AspNetCore.Mvc;

namespace Genius.PriceChecker.WebApi.Controllers;

public sealed class AgentsController : BaseCrudController<Guid, AgentRef, AgentDto, IAgentsRepository, CreateAgentRequest, UpdateAgentRequest>
{
    private readonly IAgentHandlersProvider _agentHandlersProvider;

    public AgentsController(IAgentsRepository agentsRepository, IRequestValidators requestValidators,
        IAgentHandlersProvider agentHandlersProvider)
        : base(agentsRepository, requestValidators)
    {
        _agentHandlersProvider = agentHandlersProvider.NotNull();
    }

    [HttpGet("handlers")]
    public IEnumerable<string> GetHandlers()
        => _agentHandlersProvider.GetNames();
}
