namespace Genius.PriceChecker.Core.Models;

/// <summary>
///   The scanning agent definition, detached from any persistence concerns.
/// </summary>
public sealed record ScanAgent(
    string Key,
    string Url,
    string PricePattern,
    string Handler,
    char DecimalDelimiter);

/// <summary>
///   A single source of a product to seek the price at.
/// </summary>
public sealed record ScanSource(
    Guid SourceId,
    string Argument,
    ScanAgent Agent);

/// <summary>
///   A product with its sources, prepared for the price scanning.
/// </summary>
public sealed record ScanProduct(
    Guid ProductId,
    string Name,
    ScanSource[] Sources);

/// <summary>
///   The result of seeking the price of a single product source.
/// </summary>
public readonly record struct PriceSeekResult(
    AgentHandlingStatus Status,
    Guid ProductSourceId,
    string AgentKey,
    decimal? Price);

/// <summary>
///   A snapshot of a previously found price, used to determine the scan status of a product.
/// </summary>
public readonly record struct PriceSnapshot(
    AgentHandlingStatus Status,
    decimal? Price,
    DateTimeOffset FoundDate);
