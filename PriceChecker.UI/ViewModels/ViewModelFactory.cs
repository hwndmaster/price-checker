using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface IViewModelFactory
    {
        IAgentViewModel CreateAgent(IAgentsViewModel owner, Agent agent);
        ITrackerProductViewModel CreateTrackerProduct(Product product);
    }

    [ExcludeFromCodeCoverage]
    internal sealed class ViewModelFactory : IViewModelFactory
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IProductRepository _productRepo;
        private readonly IProductPriceManager _productPriceMng;
        private readonly IProductStatusProvider _statusProvider;
        private readonly IEventBus _eventBus;
        private readonly IUserInteraction _ui;

        public ViewModelFactory(IEventBus eventBus,
            IAgentRepository agentRepo, IProductRepository productRepo,
            IProductPriceManager productPriceMng,
            IProductStatusProvider statusProvider,
            IUserInteraction ui)
        {
            _eventBus = eventBus;
            _agentRepo = agentRepo;
            _productRepo = productRepo;
            _productPriceMng = productPriceMng;
            _statusProvider = statusProvider;
            _ui = ui;
        }

        public IAgentViewModel CreateAgent(IAgentsViewModel owner, Agent agent)
        {
            return new AgentViewModel(owner, agent);
        }

        public ITrackerProductViewModel CreateTrackerProduct(Product product)
        {
            return new TrackerProductViewModel(product, _eventBus, _agentRepo,
                _productPriceMng, _productRepo, _statusProvider, _ui);
        }
    }
}
