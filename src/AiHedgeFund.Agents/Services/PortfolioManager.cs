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
        if (!_agentRegistry.TryGet<AgentReport>(agentName, out var agentFunc))
            _logger.LogWarning("Agent not found or type mismatch");
        agentFunc(state);
    }

    public void RunRiskAssessments(string agentName, TradingWorkflowState state, RiskManagerAgent riskAgent)
    {
        riskAgent.Run(state);

        // TODO

        //if (!state.RiskAssessments.ContainsKey(agentName))
        //    state.RiskAssessments[agentName] = new Dictionary<string, RiskAssessment>();

        //foreach (var kvp in state.RiskAssesments)
        //{
        //    // TODO
        //}
    }
}