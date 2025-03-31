using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents.Services;

public class PortfolioManager
{
    private readonly IAgentRegistry _agentRegistry;

    public PortfolioManager(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public async Task EvaluateAsync(string agentName, TradingWorkflowState state)
    {
        if (_agentRegistry.TryGet<TradeSignal>(agentName, out var agentFunc))
        {
            var result = await agentFunc(state);
            // Process TradeSignal
        }
        else if (_agentRegistry.TryGet<RiskAssessment>(agentName, out var riskFunc))
        {
            var riskResult = await riskFunc(state);
            // Process RiskAssessment
        }
        else
        {
            // Agent not found or type mismatch
        }
    }
}