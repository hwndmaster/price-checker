using Genius.Atom.Data.Validation;
using Genius.Atom.Web.Controllers;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Genius.PriceChecker.WebApi.Controllers;

public sealed class AgentsController : BaseCrudController<Guid, AgentRef, AgentDto, IAgentsRepository, CreateAgentRequest, UpdateAgentRequest>
{
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly ISourceUrlRecognizer _sourceUrlRecognizer;

    public AgentsController(IAgentsRepository agentsRepository, IRequestValidators requestValidators,
        IAgentHandlersProvider agentHandlersProvider, ISourceUrlRecognizer sourceUrlRecognizer)
        : base(agentsRepository, requestValidators)
    {
        _agentHandlersProvider = agentHandlersProvider.NotNull();
        _sourceUrlRecognizer = sourceUrlRecognizer.NotNull();
    }

    [HttpGet("handlers")]
    public IEnumerable<string> GetHandlers()
        => _agentHandlersProvider.GetNames();

    /// <summary>
    ///   Returns the agents that can scan the given product URL, the most specific one first,
    ///   each with the agent argument read off that URL. An empty list means no agent recognized it.
    /// </summary>
    [HttpPost("recognize")]
    public async Task<Results<Ok<RecognizedSourceDto[]>, BadRequest<string>>> RecognizeSourceUrl(
        [FromBody] RecognizeSourceUrlRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return TypedResults.BadRequest("The request message cannot be null.");
        }

        var agents = await Repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var recognizable = agents.Select(x => new RecognizableAgent(x.Id, x.Key, x.Url, x.UrlPattern));

        var recognized = _sourceUrlRecognizer.Recognize(request.Url, recognizable)
            .Select(x => new RecognizedSourceDto(x.AgentId, x.AgentKey, x.AgentArgument, x.MatchKind))
            .ToArray();

        return TypedResults.Ok(recognized);
    }
}
