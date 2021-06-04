using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Infrastructure.Events;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface IViewModelFactory
    {
        AgentViewModel CreateAgent(AgentsViewModel owner, Agent agent);
        TrackerProductViewModel CreateTrackerProduct(Product product);
    }

    public class ViewModelFactory : IViewModelFactory
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

        public AgentViewModel CreateAgent(AgentsViewModel owner, Agent agent)
        {
            return new AgentViewModel(owner, agent);
        }

        public TrackerProductViewModel CreateTrackerProduct(Product product)
        {
            return new TrackerProductViewModel(product, _eventBus, _agentRepo,
                _productPriceMng, _productRepo, _statusProvider, _ui);
        }
    }
}
