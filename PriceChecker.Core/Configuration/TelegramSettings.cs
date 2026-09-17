namespace Genius.PriceChecker.Core.Configuration;

/// <summary>
///   The configuration of the Telegram notification service, bound from the <c>Telegram</c> section.
/// </summary>
public sealed class TelegramSettings
{
    public const string SectionName = "Telegram";

    /// <summary>
    ///   The Telegram Bot API token.
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>
    ///   The identifier of the chat the notifications are sent to.
    /// </summary>
    public string? ChatId { get; set; }

    /// <summary>
    ///   Whether the integration has everything it needs to send. Absent settings are not an error:
    ///   they simply leave the notifications off, which is what an install without a bot wants.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);
}
