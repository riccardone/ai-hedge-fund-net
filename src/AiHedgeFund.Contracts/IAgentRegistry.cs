namespace AiHedgeFund.Contracts;

public interface IAgentRegistry
{
    void Register<T>(string name, Func<TradingWorkflowState, IEnumerable<T>> func);
    bool TryGet<T>(string name, out Func<TradingWorkflowState, IEnumerable<T>>? agentFunc);
    IEnumerable<string> RegisteredAgentNames { get; }
}