namespace AiHedgeFund.Contracts;

public interface IAgentRegistry
{
    void Register<T>(string name, Func<TradingWorkflowState, Task<IEnumerable<T>>> func);
    bool TryGet<T>(string name, out Func<TradingWorkflowState, Task<IEnumerable<T>>>? agentFunc);
    IEnumerable<string> RegisteredAgentNames { get; }
}