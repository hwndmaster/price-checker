using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.Atom.Infrastructure.Events;
using Genius.PriceChecker.UI.Helpers;
using Genius.Atom.Infrastructure.Commands;

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
        private readonly IAgentQueryService _agentQuery;
        private readonly IProductQueryService _productQuery;
        private readonly IProductPriceManager _productPriceMng;
        private readonly IProductStatusProvider _statusProvider;
        private readonly IEventBus _eventBus;
        private readonly ICommandBus _commandBus;
        private readonly IUserInteraction _ui;

        public ViewModelFactory(IEventBus eventBus,
            ICommandBus commandBus,
            IAgentQueryService agentQuery, IProductQueryService productQuery,
            IProductPriceManager productPriceMng,
            IProductStatusProvider statusProvider,
            IUserInteraction ui)
        {
            _eventBus = eventBus;
            _commandBus = commandBus;
            _agentQuery = agentQuery;
            _productQuery = productQuery;
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
            return new TrackerProductViewModel(product, _eventBus, _commandBus, _agentQuery,
                _productQuery, _statusProvider, _ui);
        }
    }
}
