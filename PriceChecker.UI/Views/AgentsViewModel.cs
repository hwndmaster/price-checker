using Genius.Atom.UI.Forms;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.Atom.Infrastructure.Tasks;

namespace Genius.PriceChecker.UI.Views;

public interface IAgentsViewModel : ITabViewModel
{
    DelayedObservableCollection<IAgentViewModel> Agents { get; }
}

internal sealed class AgentsViewModel : TabViewModelBase, IAgentsViewModel, IHasDirtyFlag
{
    private readonly ICommandBus _commandBus;
    private readonly IViewModelFactory _vmFactory;

    public AgentsViewModel(IAgentQueryService agentQuery, IViewModelFactory vmFactory,
        IUserInteraction ui, ICommandBus commandBus, IAgentHandlersProvider agentHandlersProvider)
    {
        // Dependencies:
        _commandBus = commandBus.NotNull();
        _vmFactory = vmFactory.NotNull();

        // Member initialization:
        AgentHandlers = agentHandlersProvider.GetNames().ToList();

        // Actions:
        AddAgentCommand = new ActionCommand(_ =>
        {
            Agents.Add(CreateAgentViewModel(null));
            IsDirty = true;
        });

        DeleteAgentCommand = new ActionCommand(async _ =>
        {
            var selectedAgent = Agents.FirstOrDefault(x => x.IsSelected);
            if (selectedAgent == null)
            {
                ui.ShowWarning("No agent selected.");
                return;
            }
            if (!ui.AskForConfirmation($"Are you sure you want to delete the selected '{selectedAgent.Key}' agent?", "Delete agent"))
                return;

            Agents.Remove(selectedAgent);
            if (selectedAgent.Id.HasValue)
            {
                await commandBus.SendAsync(new AgentDeleteCommand(selectedAgent.Id.Value));
            }
        });

        CommitAgentsCommand = new ActionCommand(async _ => await CommitAgentsAsync(),
            _ => IsDirty && !HasErrors);

        ResetChangesCommand = new ActionCommand(_ => {
            foreach (var agent in Agents)
            {
                agent.ResetForm();
            }
            SetNotDirty();
        }, _ => IsDirty);

        // Final preparation:
        FetchAgentsAsync(agentQuery).RunAndForget();
    }

    private async Task FetchAgentsAsync(IAgentQueryService agentQuery)
    {
        var agents = await agentQuery.GetAllAsync();
        var agentVms = agents
            .OrderBy(x => x.Key)
            .Select(x => CreateAgentViewModel(x));
        Agents.ReplaceItems(agentVms);
    }

    private IAgentViewModel CreateAgentViewModel(Agent? x)
    {
        var agentVm = _vmFactory.CreateAgent(this, x);
        agentVm.WhenChanged(x => x.IsDirty, x => IsDirty = IsDirty || x);
        return agentVm;
    }

    private async Task CommitAgentsAsync()
    {
        if (HasErrors)
        {
            return;
        }

        var agents = Agents.Select(x => x.GetOrCreateEntity()).ToArray();

        await _commandBus.SendAsync(new AgentsStoreWithOverwriteCommand(agents));

        SetNotDirty();
    }

    private void SetNotDirty()
    {
        IsDirty = false;
        foreach (var agent in Agents)
        {
            agent.IsDirty = false;
        }
    }

    public DelayedObservableCollection<IAgentViewModel> Agents { get; }
        = new TypedObservableCollection<IAgentViewModel, AgentViewModel>();

    public IReadOnlyCollection<string> AgentHandlers { get; }

    public bool IsAddEditAgentVisible
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsDirty
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    public override bool HasErrors => Agents.Any(x => x.HasErrors);

    public IActionCommand AddAgentCommand { get; }
    public IActionCommand CommitAgentsCommand { get; }
    public IActionCommand DeleteAgentCommand { get; }
    public IActionCommand ResetChangesCommand { get; }
}
