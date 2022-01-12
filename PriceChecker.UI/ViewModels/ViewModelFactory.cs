using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels;

public interface IViewModelFactory
{
    IAgentViewModel CreateAgent(IAgentsViewModel owner, Agent? agent);
    ITrackerProductViewModel CreateTrackerProduct(Product? product);
}

[ExcludeFromCodeCoverage]
internal sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IAgentQueryService _agentQuery;
    private readonly IProductQueryService _productQuery;
    private readonly IProductStatusProvider _statusProvider;
    private readonly IEventBus _eventBus;
    private readonly ICommandBus _commandBus;
    private readonly IUserInteraction _ui;
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly IProductInteraction _productInteraction;

    public ViewModelFactory(IEventBus eventBus,
        ICommandBus commandBus,
        IAgentQueryService agentQuery, IProductQueryService productQuery,
        IProductStatusProvider statusProvider,
        IUserInteraction ui,
        IAgentHandlersProvider agentHandlersProvider,
        IProductInteraction productInteraction)
    {
        _eventBus = eventBus;
        _commandBus = commandBus;
        _agentQuery = agentQuery;
        _productQuery = productQuery;
        _statusProvider = statusProvider;
        _ui = ui;
        _agentHandlersProvider = agentHandlersProvider;
        _productInteraction = productInteraction;
    }

    public IAgentViewModel CreateAgent(IAgentsViewModel owner, Agent? agent)
    {
        return new AgentViewModel(owner, agent, _agentHandlersProvider);
    }

    public ITrackerProductViewModel CreateTrackerProduct(Product? product)
    {
        return new TrackerProductViewModel(product, _eventBus, _commandBus, _agentQuery,
            _productQuery, _statusProvider, _ui, _productInteraction);
    }
}
