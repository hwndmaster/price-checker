using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Core.Services.Telegram;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.WebApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genius.PriceChecker.WebApi.Tests.Services;

public sealed class PriceAlertNotifierTests
{
    private readonly ITelegramNotificationService _telegram = A.Fake<ITelegramNotificationService>();

    [Fact]
    public void From_GivenTargetReached_ThenCarriesTheRecentPrices()
    {
        // Act
        var alert = PriceAlert.From(
            Overview(lowest: 260m, recent: 289m, target: 250m),
            Overview(lowest: 249m, recent: 249m, target: 250m),
            new PriceChange(HasNewLowestPrice: true, HasReachedTarget: true, TargetPrice: 250m));

        // Assert: the target is about what the product costs now, so the recent prices are what it reports.
        Assert.NotNull(alert);
        Assert.Equal(PriceAlertKind.TargetReached, alert.Kind);
        Assert.Equal(249m, alert.NewPrice);
        Assert.Equal(289m, alert.PreviousPrice);
        Assert.Equal(250m, alert.TargetPrice);
    }

    [Fact]
    public void From_GivenNewLowestPrice_ThenCarriesTheLowestPrices()
    {
        // Act
        var alert = PriceAlert.From(
            Overview(lowest: 100m, recent: 120m),
            Overview(lowest: 89.95m, recent: 89.95m),
            new PriceChange(HasNewLowestPrice: true, HasReachedTarget: false, TargetPrice: null));

        // Assert
        Assert.NotNull(alert);
        Assert.Equal(PriceAlertKind.NewLowestPrice, alert.Kind);
        Assert.Equal(89.95m, alert.NewPrice);
        Assert.Equal(100m, alert.PreviousPrice);
        Assert.Null(alert.TargetPrice);
    }

    [Fact]
    public void From_GivenChangeNotWorthReporting_ThenNoAlert()
    {
        // Act
        var alert = PriceAlert.From(
            Overview(lowest: 260m, recent: 260m, target: 250m),
            Overview(lowest: 255m, recent: 255m, target: 250m),
            new PriceChange(HasNewLowestPrice: true, HasReachedTarget: false, TargetPrice: 250m));

        // Assert
        Assert.Null(alert);
    }

    [Fact]
    public async Task NotifyAsync_GivenSeveralAlerts_ThenOneMessageIsSentWithTargetsFirst()
    {
        // Arrange
        string? sentMessage = null;
        A.CallTo(() => _telegram.SendMessageAsync(A<string>._, A<CancellationToken>._))
            .Invokes((string message, CancellationToken _) => sentMessage = message);

        var alerts = new[]
        {
            new PriceAlert("Mouse", PriceAlertKind.NewLowestPrice, 79.95m, 89m, null,
                [new ProductSourceLinkDto("amazon", "https://example.com/p?id=1&ref=2")]),
            new PriceAlert("Headphones", PriceAlertKind.TargetReached, 1249.50m, 1289m, 1250m,
                [new ProductSourceLinkDto("bol", "https://example.com/h")]),
        };

        // Act
        await CreateSut().NotifyAsync(alerts, TestContext.Current.CancellationToken);

        // Assert
        A.CallTo(() => _telegram.SendMessageAsync(A<string>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        Assert.NotNull(sentMessage);
        Assert.Contains("🎯 <b>Headphones</b>\n€ 1,249.50 — reached the target of € 1,250.00, was € 1,289.00",
            sentMessage, StringComparison.Ordinal);
        Assert.Contains("📉 <b>Mouse</b>\n€ 79.95 — is a new lowest price, was € 89.00",
            sentMessage, StringComparison.Ordinal);
        // The target lead the message, whichever order the scan produced them in.
        Assert.True(sentMessage.IndexOf("Headphones", StringComparison.Ordinal)
            < sentMessage.IndexOf("Mouse", StringComparison.Ordinal));
        // The "&" of a query string would otherwise be read as the start of an HTML entity.
        Assert.Contains("<a href=\"https://example.com/p?id=1&amp;ref=2\">amazon</a>", sentMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotifyAsync_GivenNoAlerts_ThenNothingIsSent()
    {
        // Act
        await CreateSut().NotifyAsync([], TestContext.Current.CancellationToken);

        // Assert
        A.CallTo(() => _telegram.SendMessageAsync(A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public void BuildMessage_GivenMarkupInTheProductName_ThenItIsEscaped()
    {
        // Act
        var message = TelegramPriceAlertNotifier.BuildMessage(
            [new PriceAlert("Sony <WH-1000XM5> & co", PriceAlertKind.NewLowestPrice, 249m, null, null, [])]);

        // Assert: Telegram rejects the whole message over one unknown tag, so nothing may reach it raw.
        Assert.Contains("<b>Sony &lt;WH-1000XM5&gt; &amp; co</b>", message, StringComparison.Ordinal);
        // No previous price is known, so there is nothing to compare against.
        Assert.DoesNotContain("was", message, StringComparison.Ordinal);
    }

    private TelegramPriceAlertNotifier CreateSut()
        => new(_telegram, NullLogger<TelegramPriceAlertNotifier>.Instance);

    private static ProductOverviewDto Overview(decimal? lowest, decimal? recent, decimal? target = null)
        => new(
            new ProductRef(Guid.NewGuid()),
            "Test Product",
            Category: null,
            Description: null,
            Sources: [],
            ProductScanStatus.ScannedOk,
            StatusText: null,
            LowestPrice: lowest,
            LowestFoundDate: lowest is null ? null : DateTimeOffset.UnixEpoch,
            RecentPrice: recent,
            TargetPrice: target,
            LastScannedDate: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch);
}
