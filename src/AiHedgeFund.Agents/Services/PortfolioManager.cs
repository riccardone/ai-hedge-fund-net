using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents.Services;

public class PortfolioManager
{
    private readonly ILogger<PortfolioManager> _logger;
    private readonly IAgentRegistry _agentRegistry;

    public PortfolioManager(IAgentRegistry agentRegistry, ILogger<PortfolioManager> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    public void Evaluate(string agentName, TradingWorkflowState state)
    {
        if (_agentRegistry.TryGet<AgentReport>(agentName, out var agentFunc))
            agentFunc(state);
        else
            _logger.LogWarning("Agent not found or type mismatch");
    }

    public void RunRiskAssessments(string agentName, TradingWorkflowState state, RiskManagerAgent riskAgent)
    {
        var results = riskAgent.Run(state);

        if (!state.RiskAssessments.ContainsKey(agentName))
            state.RiskAssessments[agentName] = new Dictionary<string, RiskAssessment>();

        foreach (var kvp in results)
        {
            state.RiskAssessments[agentName][kvp.Key] = kvp.Value;
        }

        //if (state.ShowReasoning)
        //{
        //    foreach (var (ticker, assessment) in results)
        //    {
        //        var r = assessment.Reasoning;
        //        Logger.Info($"[RiskAssessment:{agentName}] {ticker} - Position: {r.CurrentPosition}, Limit: {r.PositionLimit}, Remaining: {r.RemainingLimit}, Cash: {r.AvailableCash}");
        //    }
        //}
    }
}