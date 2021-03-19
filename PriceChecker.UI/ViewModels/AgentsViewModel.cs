using System.Collections.ObjectModel;
using System.Linq;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.UI.Forms;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Helpers;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class AgentsViewModel : TabViewModelBase<AgentsViewModel>, IHasDirtyFlag
    {
        private readonly IAgentRepository _agentRepo;
        private readonly IViewModelFactory _vmFactory;

        public AgentsViewModel(IAgentRepository agentRepo, IViewModelFactory vmFactory,
            IUserInteraction ui)
        {
            _agentRepo = agentRepo;
            _vmFactory = vmFactory;

            var agentVms = agentRepo.GetAll().Select(x => CreateAgentViewModel(x));
            Agents.ReplaceItems(agentVms);

            AddAgentCommand = new ActionCommand(o =>
            {
                Agents.Add(CreateAgentViewModel(null));
                IsDirty = true;
            });

            DeleteAgentCommand = new ActionCommand(_ =>
            {
                var selectedAgent = Agents.FirstOrDefault(x => x.IsSelected);
                if (selectedAgent == null)
                {
                    ui.ShowWarning("No agent selected.");
                    return;
                }
                if (!ui.AskForConfirmation($"Are you sure you want to delete the selected '{selectedAgent.Id}' agent?", "Delete agent"))
                    return;

                Agents.Remove(selectedAgent);
                agentRepo.Delete(selectedAgent.Id);
            });

            CommitAgentsCommand = new ActionCommand(_ => CommitAgents(), _ => IsDirty && !HasErrors);

            ResetChangesCommand = new ActionCommand(_ => {
                foreach (var agent in Agents)
                {
                    agent.ResetForm();
                }
            });

            PropertiesAreInitialized = true;
        }

        private AgentViewModel CreateAgentViewModel(Agent x)
        {
            var agentVm = _vmFactory.CreateAgent(this, x);
            agentVm.WhenChanged(x => x.IsDirty, x =>
            {
                IsDirty = IsDirty || x;
            });
            return agentVm;
        }

        private void CommitAgents()
        {
            if (HasErrors)
            {
                return;
            }

            var agents = Agents.Select(x => x.CreateEntity()).ToList();
            _agentRepo.Store(agents);

            IsDirty = false;
        }

        public ObservableCollection<AgentViewModel> Agents { get; } = new ObservableCollection<AgentViewModel>();

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