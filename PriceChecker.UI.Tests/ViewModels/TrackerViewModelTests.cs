using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using AutoFixture;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.Atom.UI.Forms.TestingUtil;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ViewModels;
using Moq;
using Xunit;

namespace Genius.PriceChecker.UI.Tests.ViewModels
{
  public class TrackerViewModelTests : TestBase
    {
        private readonly Mock<IProductQueryService> _productQueryMock = new();
        private readonly Mock<UI.ViewModels.IViewModelFactory> _vmFactoryMock = new();
        private readonly Mock<IUserInteraction> _uiMock = new();
        private readonly Mock<ITrackerScanContext> _scanContextMock = new();
        private readonly Mock<ICommandBus> _commandBusMock = new();

        // Session values:
        private readonly Subject<ProductScanStartedEvent> _productScanStartedEventSubject;
        private readonly Subject<ProductScannedEvent> _productScannedEventSubject;
        private readonly Subject<ProductScanFailedEvent> _productScanFailedEventSubject;

        public TrackerViewModelTests()
        {
            _productScanStartedEventSubject = CreateEventSubject<ProductScanStartedEvent>();
            _productScannedEventSubject = CreateEventSubject<ProductScannedEvent>();
            _productScanFailedEventSubject = CreateEventSubject<ProductScanFailedEvent>();

            _vmFactoryMock.Setup(x => x.CreateTrackerProduct(It.IsAny<Product>()))
                .Returns((Product p) => Mock.Of<ITrackerProductViewModel>(x =>
                    x.Id == (p == null ? Guid.Empty : p.Id) &&
                    x.RefreshPriceCommand == Mock.Of<IActionCommand>() &&
                    x.CommitProductCommand == Mock.Of<IActionCommand>(c => c.Executed == new Subject<Unit>())));
        }

        [Fact]
        public void Constructor__RefreshOptions_are_defined_and_list_reloaded()
        {
            // Arrange
            var products = SampleProducts();

            // Act
            var sut = CreateSystemUnderTest();

            // Verify
            Assert.NotEmpty(sut.RefreshOptions);
            Assert.Equal(products.Select(x => x.Id), sut.Products.Select(x => x.Id));
        }

        [Fact]
        public void RefreshAllCommand__Enqueues_all_products_for_scan()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.IsAddEditProductVisible = true;

            // Act
            sut.RefreshAllCommand.Execute(null);

            // Verify
            Assert.False(sut.IsAddEditProductVisible);
            _scanContextMock.Verify(x => x.NotifyStarted(products.Count));
            foreach (var product in sut.Products)
            {
                Mock.Get(product.RefreshPriceCommand).Verify(x => x.Execute(null), Times.Once);
            }
        }

        [Fact]
        public void RefreshAllCommand__Enqueues_selected_products_for_scan()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.Products[0].IsSelected = true;
            sut.Products[2].IsSelected = true;

            // Act
            sut.RefreshSelectedCommand.Execute(null);

            // Verify
            _scanContextMock.Verify(x => x.NotifyStarted(2));
            foreach (var product in sut.Products)
            {
                var times = product == sut.Products[0] || product == sut.Products[2] ? Times.Once() : Times.Never();
                Mock.Get(product.RefreshPriceCommand).Verify(x => x.Execute(null), times);
            }
        }

        [Fact]
        public void OpenAddProductFlyoutCommand__When_flyout_is_closed__Flyout_shows_up()
        {
            // Arrange
            SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.IsAddEditProductVisible = false;
            sut.EditingProduct = null;

            // Act
            sut.OpenAddProductFlyoutCommand.Execute(null);

            // Verify
            Assert.True(sut.IsAddEditProductVisible);
            Assert.NotNull(sut.EditingProduct);
            Assert.Equal(Guid.Empty, sut.EditingProduct.Id);
        }

        [Fact]
        public void OpenAddProductFlyoutCommand__When_flyout_is_opened__Flyout_closed()
        {
            // Arrange
            SampleProducts();
            var sut = CreateSystemUnderTest();
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
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.OpenAddProductFlyoutCommand.Execute(null); // trigger to open flyout

            // Act
            ((Subject<Unit>)sut.EditingProduct.CommitProductCommand.Executed).OnNext(Unit.Default);

            // Verify
            Assert.False(sut.IsAddEditProductVisible);
            _productQueryMock.Verify(x => x.GetAll(), Times.Exactly(2));
        }

        [Fact]
        public void OpenEditProductFlyoutCommand__When_flyout_is_closed__Flyout_shows_up()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.IsAddEditProductVisible = false;
            sut.Products[0].IsSelected = true;
            sut.EditingProduct = null;

            // Act
            sut.OpenEditProductFlyoutCommand.Execute(null);

            // Verify
            Assert.True(sut.IsAddEditProductVisible);
            Assert.NotNull(sut.EditingProduct);
            Assert.Equal(products.First().Id, sut.EditingProduct.Id);
        }

        [Fact]
        public void OpenEditProductFlyoutCommand__When_flyout_is_opened__Flyout_closed()
        {
            // Arrange
            SampleProducts();
            var sut = CreateSystemUnderTest();
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
            var sut = CreateSystemUnderTest();
            sut.Products[0].IsSelected = true;
            sut.OpenEditProductFlyoutCommand.Execute(null); // trigger to open flyout

            // Act
            ((Subject<Unit>)sut.EditingProduct.CommitProductCommand.Executed).OnNext(Unit.Default);

            // Verify
            Assert.False(sut.IsAddEditProductVisible);
        }

        [Fact]
        public void DeleteProductCommand__User_confirmed__Flyout_closed_and_product_removed()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.Products[1].IsSelected = true;
            sut.IsAddEditProductVisible = true;
            _uiMock.Setup(x => x.AskForConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            sut.DeleteProductCommand.Execute(null);

            // Verify
            var deletedProductId = products.ElementAt(1).Id;
            Assert.False(sut.IsAddEditProductVisible);
            Assert.DoesNotContain(sut.Products, x => x.Id == deletedProductId);
            Assert.Equal(products.Count - 1, sut.Products.Count);
            _commandBusMock.Verify(x => x.SendAsync(It.Is<ProductDeleteCommand>(c => c.ProductId == deletedProductId)));
        }

        [Fact]
        public void DeleteProductCommand__User_not_confirmed__Flyout_closed_and_product_remained()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.Products[1].IsSelected = true;
            sut.IsAddEditProductVisible = true;
            _uiMock.Setup(x => x.AskForConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            // Act
            sut.DeleteProductCommand.Execute(null);

            // Verify
            var deletingProductId = products.ElementAt(1).Id;
            Assert.False(sut.IsAddEditProductVisible);
            Assert.Contains(sut.Products, x => x.Id == deletingProductId);
            Assert.Equal(products.Count, sut.Products.Count);
            _commandBusMock.Verify(x => x.SendAsync(It.IsAny<ProductDeleteCommand>()), Times.Never);
        }

        [Fact]
        public void DeleteProductCommand__No_product_selected__Flyout_closed_and_operation_cancelled()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            sut.IsAddEditProductVisible = true;

            // Act
            sut.DeleteProductCommand.Execute(null);

            // Verify
            Assert.False(sut.IsAddEditProductVisible);
            Assert.Equal(products.Count, sut.Products.Count);
            _commandBusMock.Verify(x => x.SendAsync(It.IsAny<ProductDeleteCommand>()), Times.Never);
            _uiMock.Verify(x => x.AskForConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProductScanStartedEvent_fired__Appropriate_product_changed_status()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            const int productScanningIndex = 1;

            // Act
            _productScanStartedEventSubject.OnNext(new ProductScanStartedEvent(products.ElementAt(productScanningIndex)));

            // Verify
            Assert.Equal(ProductScanStatus.Scanning, sut.Products[productScanningIndex].Status);
        }

        [Fact]
        public void ProductScannedEvent_fired__Appropriate_product_changed_status()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            var lowestUpdated = Fixture.Create<bool>();
            const int productScannedIndex = 1;

            // Act
            _productScannedEventSubject.OnNext(new ProductScannedEvent(products.ElementAt(productScannedIndex), lowestUpdated));

            // Verify
            Mock.Get(sut.Products[productScannedIndex]).Verify(x => x.Reconcile(lowestUpdated), Times.Once);
        }

        [Fact]
        public void ProductScanFailedEvent_fired__Appropriate_product_set_to_failed()
        {
            // Arrange
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            const int productFailedIndex = 1;
            var errorMessage = Fixture.Create<string>();

            // Act
            _productScanFailedEventSubject.OnNext(new ProductScanFailedEvent(products.ElementAt(productFailedIndex), errorMessage));

            // Verify
            Mock.Get(sut.Products[productFailedIndex]).Verify(x => x.SetFailed(errorMessage), Times.Once);
        }

        [Fact]
        public void Deactivated__Flyout_closed()
        {
            // Arrange
            SampleProducts();
            var sut = CreateSystemUnderTest();
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
            var products = SampleProducts();
            var sut = CreateSystemUnderTest();
            var product = sut.Products[1];
            var status = Fixture.Create<Generator<ProductScanStatus>>()
                .First(x => x != product.Status);

            // Act
            RaisePropertyChanged(Mock.Get(product), x => x.Status, status);

            // Verify
            _scanContextMock.Verify(x => x.NotifyProgressChange(status));
        }

        private TrackerViewModel CreateSystemUnderTest()
        {
            return new TrackerViewModel(EventBusMock.Object, _productQueryMock.Object,
                _vmFactoryMock.Object, _uiMock.Object, _scanContextMock.Object,
                _commandBusMock.Object);
        }

        private ICollection<Product> SampleProducts()
        {
            var products = Fixture.CreateMany<Product>().ToList();
            _productQueryMock.Setup(x => x.GetAll()).Returns(products);
            return products;
        }
    }
}
