using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Db.Models;

public sealed record Agent : EntityBase<Guid, AgentRef>
{
    public required string Key { get; init; }
    public required string Url { get; init; }
    public required string PricePattern { get; init; }
    public required string Handler { get; init; }
    public required char DecimalDelimiter { get; init; }

    /// <summary>
    ///   A regular expression matching the product URLs of this agent's site, capturing the
    ///   <see cref="ProductSource.AgentArgument"/> in a group named <c>arg</c>. Optional: an agent
    ///   without one is recognized by reverse-matching its <see cref="Url"/> template instead.
    /// </summary>
    public string? UrlPattern { get; init; }

    /// <summary>
    ///   Creates an <see cref="Agent"/> instance without linking the related entities.
    /// </summary>
    public static Agent CreateWithNoLinking(string key, string url, string pricePattern, string handler,
        char decimalDelimiter, string? urlPattern = null, DateTimeOffset? date = null, Guid? id = null)
        => new()
        {
            Key = key,
            Url = url,
            PricePattern = pricePattern,
            Handler = handler,
            DecimalDelimiter = decimalDelimiter,
            UrlPattern = urlPattern,
            DateCreated = date ?? DateTimeOffset.MinValue,
            LastModified = date ?? DateTimeOffset.MinValue,
            Id = id ?? Guid.NewGuid(),
        };
}
