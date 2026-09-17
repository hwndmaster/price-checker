using System.Net.Http.Json;
using Genius.PriceChecker.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Genius.PriceChecker.Core.Services.Telegram;

/// <summary>
///   Sends messages through the Telegram Bot API.
/// </summary>
internal sealed class TelegramNotificationService : ITelegramNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(HttpClient httpClient, IOptions<TelegramSettings> settings,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClient = httpClient.NotNull();
        _settings = settings.NotNull().Value;
        _logger = logger.NotNull();
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            _logger.LogDebug("Telegram is not configured, the notification is not sent.");
            return;
        }

        var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = _settings.ChatId,
            text = message,
            parse_mode = "HTML",
            // The price alerts carry the product links, and a preview of the first one would push the
            // rest of the message out of view in the chat list.
            disable_web_page_preview = true,
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Swallowed on purpose: the caller is a scan that has already done its work, and an
            // unreachable Telegram must not turn a successful scan into a failed one.
            _logger.LogError(ex, "Failed to send the Telegram notification.");
        }
    }
}
