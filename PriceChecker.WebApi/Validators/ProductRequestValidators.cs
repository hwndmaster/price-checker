using System.ComponentModel.DataAnnotations;
using Genius.Atom.Data.Validation;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;

namespace Genius.PriceChecker.WebApi.Validators;

public sealed class CreateProductRequestValidator : IRequestValidator<CreateProductRequest>
{
    private readonly IAgentsRepository _agentsRepository;

    public CreateProductRequestValidator(IAgentsRepository agentsRepository)
    {
        _agentsRepository = agentsRepository.NotNull();
    }

    public async Task<ValidationResult?> ValidateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new ValidationResult("Request must not be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new ValidationResult("The product name must not be empty.", [nameof(CreateProductRequest.Name)]);
        }

        if (ProductValidationHelper.ValidateTargetPrice(request.TargetPrice, nameof(CreateProductRequest.TargetPrice))
            is { } targetPriceError)
        {
            return targetPriceError;
        }

        return await ProductValidationHelper.ValidateAgentsExistAsync(_agentsRepository,
            request.Sources.Select(x => x.AgentId), cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdateProductRequestValidator : IRequestValidator<UpdateProductRequest>
{
    private readonly IAgentsRepository _agentsRepository;

    public UpdateProductRequestValidator(IAgentsRepository agentsRepository)
    {
        _agentsRepository = agentsRepository.NotNull();
    }

    public async Task<ValidationResult?> ValidateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new ValidationResult("Request must not be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new ValidationResult("The product name must not be empty.", [nameof(UpdateProductRequest.Name)]);
        }

        if (ProductValidationHelper.ValidateTargetPrice(request.TargetPrice, nameof(UpdateProductRequest.TargetPrice))
            is { } targetPriceError)
        {
            return targetPriceError;
        }

        return await ProductValidationHelper.ValidateAgentsExistAsync(_agentsRepository,
            request.Sources.Select(x => x.AgentId), cancellationToken).ConfigureAwait(false);
    }
}

internal static class ProductValidationHelper
{
    /// <summary>
    ///   An absent target price means the product tracks none; a present one has to be a price that
    ///   can actually be reached.
    /// </summary>
    public static ValidationResult? ValidateTargetPrice(decimal? targetPrice, string fieldName)
        => targetPrice is not null && targetPrice <= 0
            ? new ValidationResult("The target price must be greater than zero.", [fieldName])
            : null;

    public static async Task<ValidationResult?> ValidateAgentsExistAsync(IAgentsRepository agentsRepository,
        IEnumerable<AgentRef> agentIds, CancellationToken cancellationToken)
    {
        var distinctAgentIds = agentIds.Distinct().ToArray();
        if (distinctAgentIds.Length == 0)
        {
            return ValidationResult.Success;
        }

        var foundAgents = await agentsRepository.GetByIdsAsync(distinctAgentIds, cancellationToken).ConfigureAwait(false);
        if (foundAgents.Count() != distinctAgentIds.Length)
        {
            return new ValidationResult("One or more source agents are not known.", [nameof(CreateProductRequest.Sources)]);
        }

        return ValidationResult.Success;
    }
}
