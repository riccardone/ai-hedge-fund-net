using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents.Registry;

public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, Delegate> _agents = new();

    public void Register<T>(string name, Func<TradingWorkflowState, Task<IEnumerable<T>>> func)
    {
        _agents[name] = func;
    }

    public bool TryGet<T>(string name, out Func<TradingWorkflowState, Task<IEnumerable<T>>>? agentFunc)
    {
        if (_agents.TryGetValue(name, out var del) && del is Func<TradingWorkflowState, Task<IEnumerable<T>>> typed)
        {
            agentFunc = typed;
            return true;
        }

        agentFunc = null;
        return false;
    }

    public IEnumerable<string> RegisteredAgentNames => _agents.Keys;
}