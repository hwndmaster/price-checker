namespace Genius.PriceChecker.Core.Services.Telegram;

/// <summary>
///   Sends notifications to the configured Telegram chat.
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    ///   Sends a message to the configured Telegram chat. Does nothing when the integration is not
    ///   configured, and never throws: a notification that cannot be delivered must not fail whatever
    ///   produced it.
    /// </summary>
    /// <param name="message">The message text, in Telegram's HTML formatting.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
}
