using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents.Registry;

public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, Delegate> _agents = new();

    public void Register(string name, Action<TradingWorkflowState> action)
    {
        _agents[name] = action;
    }

    public bool TryGet<T>(string name, out Action<TradingWorkflowState>? agentAction)
    {
        if (_agents.TryGetValue(name, out var del) && del is Action<TradingWorkflowState> typed)
        {
            agentAction = typed;
            return true;
        }

        agentAction = null;
        return false;
    }

    public IEnumerable<string> RegisteredAgentNames => _agents.Keys;
}