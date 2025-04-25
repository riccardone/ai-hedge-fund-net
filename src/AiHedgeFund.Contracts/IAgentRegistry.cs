namespace AiHedgeFund.Contracts;

public interface IAgentRegistry
{
    void Register(string name, Action<TradingWorkflowState> action);
    bool TryGet<T>(string name, out Action<TradingWorkflowState>? agentAction);
    IEnumerable<string> RegisteredAgentNames { get; }
}