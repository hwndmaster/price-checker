using System.Globalization;
using System.Net;
using System.Text;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Core.Services.Telegram;
using Genius.PriceChecker.Dto;

namespace Genius.PriceChecker.WebApi.Services;

/// <summary>
///   Why a price is being reported.
/// </summary>
public enum PriceAlertKind
{
    /// <summary>The product beat its own lowest price. Reported for a product that tracks no target.</summary>
    NewLowestPrice,

    /// <summary>The product became available at or below its target price.</summary>
    TargetReached,
}

/// <summary>
///   A price change worth telling the user about outside the app.
/// </summary>
public sealed record PriceAlert(
    string ProductName,
    PriceAlertKind Kind,
    decimal NewPrice,
    decimal? PreviousPrice,
    decimal? TargetPrice,
    IReadOnlyList<ProductSourceLinkDto> Sources)
{
    /// <summary>
    ///   Turns what a scan changed about a product into the alert that reports it, or <c>null</c> when
    ///   the change is not worth reporting. Which prices the alert carries follows from the kind: a
    ///   target is about what the product currently costs, a new low about the history it just beat.
    /// </summary>
    public static PriceAlert? From(ProductOverviewDto? previous, ProductOverviewDto current, PriceChange change)
    {
        Guard.NotNull(current);

        if (!change.IsWorthReporting)
        {
            return null;
        }

        if (change.HasReachedTarget && current.RecentPrice is { } recentPrice)
        {
            return new PriceAlert(current.Name, PriceAlertKind.TargetReached, recentPrice,
                previous?.RecentPrice, change.TargetPrice, current.Sources);
        }

        if (change.HasNewLowestPrice && current.LowestPrice is { } lowestPrice)
        {
            return new PriceAlert(current.Name, PriceAlertKind.NewLowestPrice, lowestPrice,
                previous?.LowestPrice, null, current.Sources);
        }

        return null;
    }
}

public interface IPriceAlertNotifier
{
    /// <summary>
    ///   Reports everything one scan found, in a single notification.
    /// </summary>
    Task NotifyAsync(IReadOnlyCollection<PriceAlert> alerts, CancellationToken cancellationToken = default);
}

/// <summary>
///   Reports the price alerts of a scan to Telegram, as one message per scan rather than one per
///   product: a daily scan of a full catalogue would otherwise arrive as a burst of chat messages.
/// </summary>
internal sealed class TelegramPriceAlertNotifier : IPriceAlertNotifier
{
    private readonly ITelegramNotificationService _telegram;
    private readonly ILogger<TelegramPriceAlertNotifier> _logger;

    public TelegramPriceAlertNotifier(ITelegramNotificationService telegram,
        ILogger<TelegramPriceAlertNotifier> logger)
    {
        _telegram = telegram.NotNull();
        _logger = logger.NotNull();
    }

    public async Task NotifyAsync(IReadOnlyCollection<PriceAlert> alerts, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(alerts);

        if (alerts.Count == 0)
        {
            return;
        }

        // Logged before the send, and about what the scan found rather than about what was delivered:
        // an unconfigured or unreachable Telegram is handled inside the service and says so itself.
        _logger.LogInformation("The scan found {AlertCount} price alert(s) to report.", alerts.Count);
        await _telegram.SendMessageAsync(BuildMessage(alerts), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///   Composes the message in Telegram's HTML formatting. The products reaching their target lead,
    ///   because that is the price the user asked to be told about.
    /// </summary>
    internal static string BuildMessage(IReadOnlyCollection<PriceAlert> alerts)
    {
        var builder = new StringBuilder("💰 <b>Price Checker — Price Alert</b>");

        foreach (var alert in alerts.OrderBy(x => x.Kind == PriceAlertKind.TargetReached ? 0 : 1)
            .ThenBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("\n\n").Append(FormatAlert(alert));
        }

        return builder.ToString();
    }

    private static string FormatAlert(PriceAlert alert)
    {
        var icon = alert.Kind == PriceAlertKind.TargetReached ? "🎯" : "📉";
        var reason = alert.Kind == PriceAlertKind.TargetReached
            ? $"reached the target of {FormatPrice(alert.TargetPrice)}"
            : "is a new lowest price";
        var was = alert.PreviousPrice is null ? string.Empty : $", was {FormatPrice(alert.PreviousPrice)}";

        var line = $"{icon} <b>{Escape(alert.ProductName)}</b>\n{FormatPrice(alert.NewPrice)} — {reason}{was}";

        if (alert.Sources.Count == 0)
        {
            return line;
        }

        var links = alert.Sources.Select(s => $"<a href=\"{Escape(s.Url)}\">{Escape(s.AgentKey)}</a>");

        return $"{line}\n{string.Join(" · ", links)}";
    }

    // Matches how the app shows a price everywhere else, e.g. "€ 1,234.56".
    private static string FormatPrice(decimal? price)
        => price is null ? "—" : $"€ {price.Value.ToString("N2", CultureInfo.InvariantCulture)}";

    // A product named "Sony <X>" or a URL carrying a "&" would otherwise break the message markup,
    // and Telegram rejects the whole send rather than the offending tag.
    private static string Escape(string value)
        => WebUtility.HtmlEncode(value);
}
