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
    ///   Creates an <see cref="Agent"/> instance without linking the related entities.
    /// </summary>
    public static Agent CreateWithNoLinking(string key, string url, string pricePattern, string handler,
        char decimalDelimiter, DateTimeOffset? date = null, Guid? id = null)
        => new()
        {
            Key = key,
            Url = url,
            PricePattern = pricePattern,
            Handler = handler,
            DecimalDelimiter = decimalDelimiter,
            DateCreated = date ?? DateTimeOffset.MinValue,
            LastModified = date ?? DateTimeOffset.MinValue,
            Id = id ?? Guid.NewGuid(),
        };
}
