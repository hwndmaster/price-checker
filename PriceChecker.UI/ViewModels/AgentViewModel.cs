using System.ComponentModel;
using Genius.PriceChecker.Core.Models;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.UI.Validation;
using Genius.PriceChecker.Core.AgentHandlers;

namespace Genius.PriceChecker.UI.ViewModels;

public interface IAgentViewModel : IViewModel, IHasDirtyFlag, ISelectable
{
    Agent GetOrCreateEntity();
    void ResetForm();

    Guid? Id { get; }
    string Key { get; }
}

internal sealed class AgentViewModel : ViewModelBase, IAgentViewModel
{
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly IAgentsViewModel _owner;
    private Agent? _agent;

    public AgentViewModel(IAgentsViewModel owner, Agent? agent, IAgentHandlersProvider agentHandlersProvider)
    {
        _agentHandlersProvider = agentHandlersProvider;
        _owner = owner;
        _agent = agent;

        ResetForm(true);
    }

    public Agent GetOrCreateEntity()
    {
        if (_agent is null)
        {
            _agent = new Agent {
                Id = Guid.NewGuid()
            };
        }

        _agent.Key = Key;
        _agent.Url = Url;
        _agent.Handler = Handler;
        _agent.PricePattern = PricePattern;
        _agent.DecimalDelimiter = DecimalDelimiter;

        return _agent;
    }

    public void ResetForm()
    {
        ResetForm(false);
    }

    private void ResetForm(bool firstTimeInit)
    {
        if (_agent is null && !firstTimeInit)
        {
            return;
        }

        if (firstTimeInit)
            InitializeProperties(init);
        else
            init();

        void init()
        {
            Key = _agent?.Key ?? string.Empty;
            Url = _agent?.Url ?? string.Empty;
            PricePattern = _agent?.PricePattern ?? string.Empty;
            DecimalDelimiter = _agent?.DecimalDelimiter ?? '.';
            Handler = _agent?.Handler ?? _agentHandlersProvider.GetDefaultHandlerName();
        }
    }

    [Browsable(false)]
    public IEnumerable<string> UsedKeys => _owner.Agents.Select(x => x.Key);

    [Browsable(false)]
    public Guid? Id => _agent?.Id;

    public bool IsDirty
    {
        get => GetOrDefault(false);
        set => RaiseAndSetIfChanged(value);
    }

    [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
    [ValidationRule(typeof(MustBeUniqueValidationRule), nameof(UsedKeys))]
    public string Key
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    [Greedy]
    [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
    public string Url
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    [SelectFromList(nameof(AgentsViewModel.AgentHandlers), fromOwnerContext: true)]
    public string Handler
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    [Greedy]
    [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
    public string PricePattern
    {
        get => GetOrDefault(string.Empty);
        set => RaiseAndSetIfChanged(value);
    }

    public char DecimalDelimiter
    {
        get => GetOrDefault('.');
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }
}
