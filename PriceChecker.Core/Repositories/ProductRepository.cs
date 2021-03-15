using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.Core.Messages;

namespace Genius.PriceChecker.Core.Repositories
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAll();
        Product FindById(Guid productId);
        void Delete(Guid productId);
        void Store(Product product);
    }

    internal sealed class ProductRepository : IProductRepository
    {
        private readonly IEventBus _eventBus;
        private readonly IPersister _persister;
        private readonly ILogger<ProductRepository> _logger;

        private const string FILENAME = @".\products.json";
        private readonly List<Product> _products = null;

        public ProductRepository(IEventBus eventBus, IPersister persister, ILogger<ProductRepository> logger)
        {
            _eventBus = eventBus;
            _persister = persister;
            _logger = logger;

            _products = _persister.Load<Product>(FILENAME).ToList();
        }

        public IEnumerable<Product> GetAll()
        {
            return _products;
        }

        public Product FindById(Guid productId)
        {
            return _products.FirstOrDefault(x => x.Id == productId);
        }

        public void Delete(Guid productId)
        {
            var product = _products.FirstOrDefault(x => x.Id == productId);
            if (product == null)
            {
                _logger.LogWarning($"Cannot find product '{productId}' to delete");
                return;
            }

            _products.Remove(product);

            _persister.Store(FILENAME, _products);
        }

        public void Store(Product product)
        {
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
            }

            if (!_products.Any(x => x.Id == product.Id))
            {
                _logger.LogTrace($"New product '{product.Name}' added");
                _products.Add(product);

                _eventBus.Publish(new ProductAddedEvent(product));
            }
            else
            {
                _eventBus.Publish(new ProductUpdatedEvent(product));
            }

            _persister.Store(FILENAME, _products);
        }
    }
}
