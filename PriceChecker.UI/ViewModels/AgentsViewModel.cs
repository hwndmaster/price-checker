using System.Collections.ObjectModel;
using System.Linq;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.UI.Helpers;
using Genius.Atom.Infrastructure.Commands;
using Genius.PriceChecker.Core.Commands;
using System.Threading.Tasks;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface IAgentsViewModel : ITabViewModel
    {
        ObservableCollection<IAgentViewModel> Agents { get; }
    }

    internal sealed class AgentsViewModel : TabViewModelBase, IAgentsViewModel, IHasDirtyFlag
    {
        private readonly ICommandBus _commandBus;
        private readonly IViewModelFactory _vmFactory;

        public AgentsViewModel(IAgentQueryService agentQuery, IViewModelFactory vmFactory,
            IUserInteraction ui, ICommandBus commandBus)
        {
            _commandBus = commandBus;
            _vmFactory = vmFactory;

            var agentVms = agentQuery.GetAll().OrderBy(x => x.Key).Select(x => CreateAgentViewModel(x));
            Agents.ReplaceItems(agentVms);

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

            CommitAgentsCommand = new ActionCommand(_ => CommitAgents(),
                _ => IsDirty && !HasErrors);

            ResetChangesCommand = new ActionCommand(_ => {
                foreach (var agent in Agents)
                {
                    agent.ResetForm();
                }
                IsDirty = false;
            }, _ => IsDirty);
        }

        private IAgentViewModel CreateAgentViewModel(Agent x)
        {
            var agentVm = _vmFactory.CreateAgent(this, x);
            agentVm.WhenChanged(x => x.IsDirty, x => IsDirty = IsDirty || x);
            return agentVm;
        }

        private async Task CommitAgents()
        {
            if (HasErrors)
            {
                return;
            }

            var agents = Agents.Select(x => x.GetOrCreateEntity()).ToArray();

            await _commandBus.SendAsync(new AgentsStoreWithOverwriteCommand(agents));

            IsDirty = false;
        }

        public ObservableCollection<IAgentViewModel> Agents { get; }
            = new TypedObservableList<IAgentViewModel, AgentViewModel>();

        public bool IsAddEditAgentVisible
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        public bool IsDirty { get; set; }

        public override bool HasErrors => Agents.Any(x => x.HasErrors);

        public IActionCommand AddAgentCommand { get; }
        public IActionCommand CommitAgentsCommand { get; }
        public IActionCommand DeleteAgentCommand { get; }
        public IActionCommand ResetChangesCommand { get; }
    }
}