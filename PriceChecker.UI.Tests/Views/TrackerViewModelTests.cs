using System.ComponentModel;
using System.Reactive;
using System.Reactive.Subjects;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.TestingUtil.Commands;
using Genius.Atom.Infrastructure.TestingUtil.Events;
using Genius.Atom.Infrastructure.Threading;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.TestingUtil;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.Views;
using WinRT;

namespace Genius.PriceChecker.UI.Tests.Views;

public class TrackerViewModelTests
{
    private readonly Fixture _fixture = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly IProductQueryService _fakeProductQuery = A.Fake<IProductQueryService>();
    private readonly IViewModelFactory _fakeVmFactory = A.Fake<IViewModelFactory>();
    private readonly IUserInteraction _fakeUi = A.Fake<IUserInteraction>();
    private readonly ITrackerScanContext _fakeScanContext = A.Fake<ITrackerScanContext>();
    private readonly FakeCommandBus _commandBus = new();

    public TrackerViewModelTests()
    {
        A.CallTo(() => _fakeVmFactory.CreateTrackerProduct(A<Product>.Ignored))
            .ReturnsLazily((Product p) => {
                var commitProductCommand = A.Fake<IActionCommand>();
                A.CallTo(() => commitProductCommand.Executed).Returns(new Subject<bool>());

                var vm = A.Fake<ITrackerProductViewModel>();
                A.CallTo(() => vm.Id).Returns(p == null ? Guid.Empty : p.Id);
                A.CallTo(() => vm.RefreshPriceCommand).Returns(A.Fake<IActionCommand>());
                A.CallTo(() => vm.CommitProductCommand).Returns(commitProductCommand);
                A.CallToSet(() => vm.Status)
                    .Invokes((ProductScanStatus status) =>
                    {
                        object? statusObj;
                        A.CallTo(() => vm.Status).Returns(status);
                        A.CallTo(() => vm.TryGetPropertyValue(nameof(vm.Status), out statusObj))
                            .Returns(true)
                            .AssignsOutAndRefParameters(status);
                        vm.PropertyChanged += Raise.FreeForm.With(vm, new PropertyChangedEventArgs(nameof(vm.Status)));
                    });
                return vm;
            });
    }

    [Fact]
    public void Constructor__RefreshOptions_are_defined_and_list_reloaded()
    {
        // Arrange
        var products = SampleProducts();

        // Act
        using var sut = CreateSystemUnderTest();

        // Verify
        Assert.NotEmpty(sut.RefreshOptions);
        Assert.Equal(products, sut.Products.Select(x => x.Id!.Value));
    }

    [Fact]
    public void RefreshAllCommand__Enqueues_all_products_for_scan()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = true;

        // Act
        sut.RefreshAllCommand.Execute(null);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
        A.CallTo(() => _fakeScanContext.NotifyStarted(products.Count)).MustHaveHappenedOnceExactly();
        foreach (var product in sut.Products)
        {
            A.CallTo(() => product.RefreshPriceCommand.Execute(null)).MustHaveHappenedOnceExactly();
        }
    }

    [Fact]
    public void RefreshAllCommand__Enqueues_selected_products_for_scan()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.Products[0].IsSelected = true;
        sut.Products[2].IsSelected = true;

        // Act
        sut.RefreshSelectedCommand.Execute(null);

        // Verify
        A.CallTo(() => _fakeScanContext.NotifyStarted(2)).MustHaveHappenedOnceExactly();
        foreach (var product in sut.Products)
        {
            var times = product == sut.Products[0] || product == sut.Products[2] ? 1 : 0;
            A.CallTo(() => product.RefreshPriceCommand.Execute(null)).MustHaveHappened(times, Times.Exactly);
        }
    }

    [Fact]
    public void OpenAddProductFlyoutCommand__When_flyout_is_closed__Flyout_shows_up()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = false;
        sut.EditingProduct = null;

        // Act
        sut.OpenAddProductFlyoutCommand.Execute(null);

        // Verify
        Assert.True(sut.IsAddEditProductVisible);
        Assert.NotNull(sut.EditingProduct);
        Assert.Equal(Guid.Empty, sut.EditingProduct!.Id);
    }

    [Fact]
    public void OpenAddProductFlyoutCommand__When_flyout_is_opened__Flyout_closed()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = true;

        // Act
        sut.OpenAddProductFlyoutCommand.Execute(null);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
    }

    [Fact]
    public void OpenAddProductFlyoutCommand__Product_committed__List_reloaded_and_flyout_closed()
    {
        // Arrange
        TestModule.Initialize();
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.OpenAddProductFlyoutCommand.Execute(null); // trigger to open flyout

        // Act
        sut.EditingProduct!.CommitProductCommand.Execute(null);
        ((Subject<bool>)sut.EditingProduct!.CommitProductCommand.Executed).OnNext(true);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
        A.CallTo(() => _fakeProductQuery.GetAllAsync()).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public void OpenEditProductFlyoutCommand__When_flyout_is_closed__Flyout_shows_up()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = false;
        sut.Products[0].IsSelected = true;
        sut.EditingProduct = null;

        // Act
        sut.OpenEditProductFlyoutCommand.Execute(null);

        // Verify
        Assert.True(sut.IsAddEditProductVisible);
        Assert.NotNull(sut.EditingProduct);
        Assert.Equal(products.First(), sut.EditingProduct!.Id);
    }

    [Fact]
    public void OpenEditProductFlyoutCommand__When_flyout_is_opened__Flyout_closed()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = true;

        // Act
        sut.OpenEditProductFlyoutCommand.Execute(null);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
    }

    [Fact]
    public void OpenEditProductFlyoutCommand__Product_committed__Flyout_closed()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.Products[0].IsSelected = true;
        sut.OpenEditProductFlyoutCommand.Execute(null); // trigger to open flyout

        // Act
        ((Subject<bool>)sut.EditingProduct!.CommitProductCommand.Executed).OnNext(true);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
    }

    [Fact]
    public void DeleteProductCommand__User_confirmed__Flyout_closed_and_product_removed()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.Products[1].IsSelected = true;
        sut.IsAddEditProductVisible = true;
        A.CallTo(() => _fakeUi.AskForConfirmation(A<string>.Ignored, A<string>.Ignored)).Returns(true);

        // Act
        sut.DeleteProductCommand.Execute(null);

        // Verify
        var deletedProductId = products.ElementAt(1);
        Assert.False(sut.IsAddEditProductVisible);
        Assert.DoesNotContain(sut.Products, x => x.Id == deletedProductId);
        Assert.Equal(products.Count - 1, sut.Products.Count);
        _commandBus.AssertSingleCommand<ProductDeleteCommand>(x => x.ProductId == deletedProductId);
    }

    [Fact]
    public void DeleteProductCommand__User_not_confirmed__Flyout_closed_and_product_remained()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.Products[1].IsSelected = true;
        sut.IsAddEditProductVisible = true;
        A.CallTo(() => _fakeUi.AskForConfirmation(A<string>.Ignored, A<string>.Ignored)).Returns(false);

        // Act
        sut.DeleteProductCommand.Execute(null);

        // Verify
        var deletingProductId = products.ElementAt(1);
        Assert.False(sut.IsAddEditProductVisible);
        Assert.Contains(sut.Products, x => x.Id == deletingProductId);
        Assert.Equal(products.Count, sut.Products.Count);
        _commandBus.AssertNoCommandOfType<ProductDeleteCommand>();
    }

    [Fact]
    public void DeleteProductCommand__No_product_selected__Flyout_closed_and_operation_cancelled()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = true;

        // Act
        sut.DeleteProductCommand.Execute(null);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
        Assert.Equal(products.Count, sut.Products.Count);
        _commandBus.AssertNoCommandOfType<ProductDeleteCommand>();
        A.CallTo(() => _fakeUi.AskForConfirmation(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
    }

    [Fact]
    public void ProductScanStartedEvent_fired__Appropriate_product_changed_status()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        const int productScanningIndex = 1;

        // Act
        _eventBus.Publish(new ProductScanStartedEvent(products.ElementAt(productScanningIndex)));

        // Verify
        Assert.Equal(ProductScanStatus.Scanning, sut.Products[productScanningIndex].Status);
    }

    [Fact]
    public void ProductScannedEvent_fired__Appropriate_product_changed_status()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        var status = _fixture.Create<ProductScanStatus>();
        const int productScannedIndex = 1;

        // Act
        _eventBus.Publish(new ProductScannedEvent(products.ElementAt(productScannedIndex), status));

        // Verify
        A.CallTo(() => sut.Products[productScannedIndex].Reconcile(status)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ProductScanFailedEvent_fired__Appropriate_product_set_to_failed()
    {
        // Arrange
        var products = SampleProducts();
        using var sut = CreateSystemUnderTest();
        const int productFailedIndex = 1;
        var errorMessage = _fixture.Create<string>();

        // Act
        _eventBus.Publish(new ProductScanFailedEvent(products.ElementAt(productFailedIndex), errorMessage));

        // Verify
        A.CallTo(() => sut.Products[productFailedIndex].SetFailed(errorMessage)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Deactivated__Flyout_closed()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        sut.IsAddEditProductVisible = true;

        // Act
        sut.Deactivated.Execute(null);

        // Verify
        Assert.False(sut.IsAddEditProductVisible);
    }

    [Fact]
    public void Product_status_changed__Scan_context_notified()
    {
        // Arrange
        SampleProducts();
        using var sut = CreateSystemUnderTest();
        var product = sut.Products[1];
        var status = _fixture.Create<Generator<ProductScanStatus>>()
            .First(x => x != product.Status);

        // Act
        product.Status = status;

        // Verify
        A.CallTo(() => _fakeScanContext.NotifyProgressChange(status)).MustHaveHappenedOnceExactly();
    }

    private TrackerViewModel CreateSystemUnderTest()
    {
        return new TrackerViewModel(_eventBus, _fakeProductQuery,
            _fakeVmFactory, new FakeUiDispatcher(), _fakeUi, _fakeScanContext, _commandBus);
    }

    private ICollection<Guid> SampleProducts()
    {
        var products = _fixture.CreateMany<Product>().ToList();
        A.CallTo(() => _fakeProductQuery.GetAllAsync()).Returns(products.AsEnumerable());
        return products.ConvertAll(x => x.Id);
    }
}
