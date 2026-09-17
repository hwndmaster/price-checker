using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Db.Repositories;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.WebApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genius.PriceChecker.WebApi.Tests.Services;

public sealed class ScanOrchestratorTests
{
    private static readonly ProductRef ProductId = new(Guid.NewGuid());
    private static readonly Guid SourceId = Guid.NewGuid();

    private readonly IProductsRepository _productsRepository = A.Fake<IProductsRepository>();
    // Faked rather than real: what the evaluation itself decides is PriceChangeEvaluatorTests' subject,
    // whereas this one is about what the orchestrator does with the verdict.
    private readonly IPriceChangeEvaluator _priceChangeEvaluator = A.Fake<IPriceChangeEvaluator>();
    private readonly IPriceAlertNotifier _alertNotifier = A.Fake<IPriceAlertNotifier>();
    private readonly IScanSessionRunner _sessionRunner = A.Fake<IScanSessionRunner>();
    private readonly IScanNotifier _scanNotifier = A.Fake<IScanNotifier>();

    [Fact]
    public async Task ScanAsync_GivenScheduledScanWithAReportablePrice_ThenTheAlertIsReported()
    {
        // Arrange
        SetupScanOf(previousLowest: 100m, newLowest: 89.95m, hasNewLowestPrice: true);
        IReadOnlyCollection<PriceAlert>? reported = null;
        A.CallTo(() => _alertNotifier.NotifyAsync(A<IReadOnlyCollection<PriceAlert>>._, A<CancellationToken>._))
            .Invokes((IReadOnlyCollection<PriceAlert> alerts, CancellationToken _) => reported = alerts);

        // Act
        await CreateSut().ScanAsync([ProductId], ScanTrigger.Scheduled, TestContext.Current.CancellationToken);

        // Assert
        A.CallTo(() => _alertNotifier.NotifyAsync(A<IReadOnlyCollection<PriceAlert>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        Assert.NotNull(reported);
        var alert = Assert.Single(reported);
        Assert.Equal(PriceAlertKind.NewLowestPrice, alert.Kind);
        Assert.Equal(89.95m, alert.NewPrice);
    }

    [Fact]
    public async Task ScanAsync_GivenManualScanWithAReportablePrice_ThenNothingIsReported()
    {
        // Arrange
        SetupScanOf(previousLowest: 100m, newLowest: 89.95m, hasNewLowestPrice: true);

        // Act
        await CreateSut().ScanAsync([ProductId], ScanTrigger.Manual, TestContext.Current.CancellationToken);

        // Assert: the user started this scan and is watching it in the app, where the new price is
        // already pushed to the list over the scan hub.
        A.CallTo(() => _alertNotifier.NotifyAsync(A<IReadOnlyCollection<PriceAlert>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _scanNotifier.ProductScanFinishedAsync(A<ProductOverviewDto>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ScanAsync_GivenScheduledScanWithNothingWorthReporting_ThenNothingIsReported()
    {
        // Arrange
        SetupScanOf(previousLowest: 100m, newLowest: 100m, hasNewLowestPrice: false);

        // Act
        await CreateSut().ScanAsync([ProductId], ScanTrigger.Scheduled, TestContext.Current.CancellationToken);

        // Assert
        A.CallTo(() => _alertNotifier.NotifyAsync(A<IReadOnlyCollection<PriceAlert>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ScanAsync_GivenAFailingNotifier_ThenTheScanStillSucceeds()
    {
        // Arrange
        SetupScanOf(previousLowest: 100m, newLowest: 89.95m, hasNewLowestPrice: true);
        A.CallTo(() => _alertNotifier.NotifyAsync(A<IReadOnlyCollection<PriceAlert>>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Telegram is down"));

        // Act
        await CreateSut().ScanAsync([ProductId], ScanTrigger.Scheduled, TestContext.Current.CancellationToken);

        // Assert: the prices are already stored, so a notification that cannot be delivered must not
        // surface as a failed scan — the product is reported as scanned all the same.
        A.CallTo(() => _scanNotifier.ProductScanFinishedAsync(A<ProductOverviewDto>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _scanNotifier.ProductScanFailedAsync(A<ProductRef>._, A<string>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    ///   Arranges one product whose single source is scanned once, moving its lowest price from
    ///   <paramref name="previousLowest"/> to <paramref name="newLowest"/>, with the evaluation
    ///   verdict the orchestrator is to act on.
    /// </summary>
    private void SetupScanOf(decimal previousLowest, decimal newLowest, bool hasNewLowestPrice)
    {
        A.CallTo(() => _priceChangeEvaluator.Evaluate(A<ProductOverviewDto?>._, A<ProductOverviewDto>._))
            .Returns(new PriceChange(hasNewLowestPrice, HasReachedTarget: false, TargetPrice: null));

        var scanProduct = new ScanProduct(ProductId.Id, "Test Product",
            [new ScanSource(SourceId, "B000123", new ScanAgent("test-agent", "https://example.com/{0}", "p", "SimpleRegex", '.'))]);

        A.CallTo(() => _productsRepository.GetScanProductsAsync(A<IEnumerable<ProductRef>?>._, A<CancellationToken>._))
            .Returns([scanProduct]);

        // The overview is read once before the results are stored and once after, so the two calls
        // return the prices from before and after the scan in turn.
        A.CallTo(() => _productsRepository.GetOverviewByIdAsync(ProductId, A<CancellationToken>._))
            .ReturnsNextFromSequence(Overview(previousLowest), Overview(newLowest));

        A.CallTo(() => _sessionRunner.RunAsync(A<IReadOnlyCollection<ScanProduct>>._, A<IScanSessionObserver>._, A<CancellationToken>._))
            .ReturnsLazily(async (IReadOnlyCollection<ScanProduct> products, IScanSessionObserver observer, CancellationToken token) =>
            {
                foreach (var product in products)
                {
                    await observer.OnProductStartedAsync(product, token).ConfigureAwait(false);
                    await observer.OnProductScannedAsync(product,
                        [new PriceSeekResult(AgentHandlingStatus.Success, SourceId, "test-agent", newLowest)], token).ConfigureAwait(false);
                }
            });
    }

    private ScanOrchestrator CreateSut()
    {
        // A real container, so that what the orchestrator resolves per scope is what it would resolve
        // at run time.
        var services = new ServiceCollection()
            .AddSingleton(_productsRepository)
            .AddSingleton(_alertNotifier)
            .BuildServiceProvider();

        return new ScanOrchestrator(
            services.GetRequiredService<IServiceScopeFactory>(),
            _sessionRunner,
            new ScanContext(),
            _scanNotifier,
            _priceChangeEvaluator,
            NullLoggerFactory.Instance);
    }

    private static ProductOverviewDto Overview(decimal lowestPrice)
        => new(
            ProductId,
            "Test Product",
            Category: null,
            Description: null,
            Sources: [new ProductSourceLinkDto("test-agent", "https://example.com/B000123")],
            ProductScanStatus.ScannedOk,
            StatusText: null,
            LowestPrice: lowestPrice,
            LowestFoundDate: DateTimeOffset.UnixEpoch,
            RecentPrice: lowestPrice,
            TargetPrice: null,
            LastScannedDate: DateTimeOffset.UnixEpoch,
            LastModified: DateTimeOffset.UnixEpoch);
}
