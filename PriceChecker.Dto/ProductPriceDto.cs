using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Dto;

public sealed record ProductPriceDto(
    ProductPriceRef Id,
    ProductSourceRef ProductSourceId,
    AgentHandlingStatus Status,
    decimal? Price,
    DateTimeOffset FoundDate);
