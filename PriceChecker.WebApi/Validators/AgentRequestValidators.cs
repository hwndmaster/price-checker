using System.ComponentModel.DataAnnotations;
using Genius.Atom.Data.Validation;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;

namespace Genius.PriceChecker.WebApi.Validators;

public sealed class CreateAgentRequestValidator : IRequestValidator<CreateAgentRequest>
{
    private readonly IAgentsRepository _agentsRepository;
    private readonly IAgentHandlersProvider _agentHandlersProvider;

    public CreateAgentRequestValidator(IAgentsRepository agentsRepository, IAgentHandlersProvider agentHandlersProvider)
    {
        _agentsRepository = agentsRepository.NotNull();
        _agentHandlersProvider = agentHandlersProvider.NotNull();
    }

    public async Task<ValidationResult?> ValidateAsync(CreateAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new ValidationResult("Request must not be null.");
        }

        return AgentValidationHelper.ValidateCommonFields(request.Key, request.Url, request.PricePattern,
                request.Handler, request.DecimalDelimiter, _agentHandlersProvider)
            ?? await AgentValidationHelper.ValidateKeyUniquenessAsync(_agentsRepository, request.Key,
                excludedAgentId: null, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdateAgentRequestValidator : IRequestValidator<UpdateAgentRequest>
{
    private readonly IAgentsRepository _agentsRepository;
    private readonly IAgentHandlersProvider _agentHandlersProvider;

    public UpdateAgentRequestValidator(IAgentsRepository agentsRepository, IAgentHandlersProvider agentHandlersProvider)
    {
        _agentsRepository = agentsRepository.NotNull();
        _agentHandlersProvider = agentHandlersProvider.NotNull();
    }

    public async Task<ValidationResult?> ValidateAsync(UpdateAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new ValidationResult("Request must not be null.");
        }

        return AgentValidationHelper.ValidateCommonFields(request.Key, request.Url, request.PricePattern,
                request.Handler, request.DecimalDelimiter, _agentHandlersProvider)
            ?? await AgentValidationHelper.ValidateKeyUniquenessAsync(_agentsRepository, request.Key,
                request.Id, cancellationToken).ConfigureAwait(false);
    }
}

internal static class AgentValidationHelper
{
    public static ValidationResult? ValidateCommonFields(string key, string url, string pricePattern,
        string handler, string decimalDelimiter, IAgentHandlersProvider agentHandlersProvider)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ValidationResult("The agent key must not be empty.", [nameof(CreateAgentRequest.Key)]);
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return new ValidationResult("The agent URL must not be empty.", [nameof(CreateAgentRequest.Url)]);
        }

        if (string.IsNullOrWhiteSpace(pricePattern))
        {
            return new ValidationResult("The price pattern must not be empty.", [nameof(CreateAgentRequest.PricePattern)]);
        }

        if (decimalDelimiter?.Length != 1)
        {
            return new ValidationResult("The decimal delimiter must be a single character.", [nameof(CreateAgentRequest.DecimalDelimiter)]);
        }

        if (agentHandlersProvider.FindByName(handler) is null)
        {
            return new ValidationResult($"The handler '{handler}' is not known.", [nameof(CreateAgentRequest.Handler)]);
        }

        return ValidationResult.Success;
    }

    public static async Task<ValidationResult?> ValidateKeyUniquenessAsync(IAgentsRepository agentsRepository,
        string key, AgentRef? excludedAgentId, CancellationToken cancellationToken)
    {
        if (await agentsRepository.ExistsWithKeyAsync(key, excludedAgentId, cancellationToken).ConfigureAwait(false))
        {
            return new ValidationResult($"An agent with key '{key}' already exists.", [nameof(CreateAgentRequest.Key)]);
        }

        return ValidationResult.Success;
    }
}
