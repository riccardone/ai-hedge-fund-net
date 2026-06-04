using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents.Registry;

public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, IAgent> _agents = new();

    public void Register(IAgent agent) => _agents[agent.Key] = agent;

    public bool TryGet(string key, out IAgent? agent) => _agents.TryGetValue(key, out agent);

    public IEnumerable<IAgent> All => _agents.Values;
}