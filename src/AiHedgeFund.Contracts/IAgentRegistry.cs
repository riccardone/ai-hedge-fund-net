namespace AiHedgeFund.Contracts;

public interface IAgentRegistry
{
    void Register(IAgent agent);
    bool TryGet(string key, out IAgent? agent);
    IEnumerable<IAgent> All { get; }
}