namespace AiHedgeFund.Contracts;

public interface IAgent
{
    string Key { get; }
    string DisplayName { get; }
    AgentResult Analyze(AgentInput input);
}
