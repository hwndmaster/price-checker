namespace Genius.PriceChecker.Core.AgentHandlers;

public interface IAgentHandlersProvider
{
    IEnumerable<IAgentHandler> GetHandlers();
    IEnumerable<string> GetNames();
    string GetDefaultHandlerName();
    IAgentHandler? FindByName(string agentHandlerName);
}

internal sealed class AgentHandlersProvider : IAgentHandlersProvider
{
    private readonly List<IAgentHandler> _agentHandlers;

    public AgentHandlersProvider(IEnumerable<IAgentHandler> agentHandlers)
    {
        _agentHandlers = agentHandlers.ToList();
    }

    public IEnumerable<IAgentHandler> GetHandlers()
    {
        return _agentHandlers;
    }

    public IEnumerable<string> GetNames()
    {
        return _agentHandlers.ConvertAll(x => x.GetType().Name);
    }

    public string GetDefaultHandlerName()
    {
        return nameof(SimpleRegex);
    }

    public IAgentHandler? FindByName(string agentHandlerName)
        => _agentHandlers.Find(x => x.GetType().Name == agentHandlerName);
}
