using System;
using System.Collections.Generic;
using System.Linq;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.Atom.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Repositories
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAll();
        Product FindById(Guid productId);
        void Delete(Guid productId);
        void DropPrices(Product product);
        void Store(Product product);
    }

    internal sealed class ProductRepository : IProductRepository
    {
        private readonly IEventBus _eventBus;
        private readonly IPersister _persister;
        private readonly IAgentRepository _agentRepo;
        private readonly ILogger<ProductRepository> _logger;

        private const string FILENAME = @".\products.json";
        private readonly List<Product> _products = null;

        public ProductRepository(IEventBus eventBus, IPersister persister,
            IAgentRepository agentRepo,
            ILogger<ProductRepository> logger)
        {
            _eventBus = eventBus;
            _persister = persister;
            _agentRepo = agentRepo;
            _logger = logger;

            _products = _persister.LoadCollection<Product>(FILENAME).ToList();

            FillupRelations();
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

        public void DropPrices(Product product)
        {
            product.Lowest = null;
            product.Recent = new ProductPrice[0];
            Store(product);
        }

        public void Store(Product product)
        {
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
            }

            FillupRelations(product);

            if (!_products.Any(x => x.Id == product.Id))
            {
                _products.Add(product);

                _logger.LogTrace($"New product '{product.Name}' added");
                _eventBus.Publish(new ProductAddedEvent(product));
            }
            else
            {
                _eventBus.Publish(new ProductUpdatedEvent(product));
            }

            _persister.Store(FILENAME, _products);

            _logger.LogInformation($"Products updated.");
        }

        private void FillupRelations()
        {
            foreach (var product in _products)
            {
                FillupRelations(product);
            }
        }

        private void FillupRelations(Product product)
        {
            var sourcesDict = product.Sources.ToDictionary(x => x.Id);

            foreach (var productSource in product.Sources)
            {
                productSource.Product = product;
                productSource.Agent = _agentRepo.FindById(productSource.AgentId);
            }
            foreach (var productPrice in product.Recent)
            {
                productPrice.ProductSource = sourcesDict[productPrice.ProductSourceId];
            }
        }
    }
}
