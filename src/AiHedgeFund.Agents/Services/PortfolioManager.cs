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

    public void RunRiskAssessments(TradingWorkflowState state, RiskManagerAgent riskAgent)
    {
        riskAgent.Run(state);

        foreach (var agentEntry in state.AnalystSignals)
        {
            foreach (var tickerEntry in agentEntry.Value)
            {
                var agentReport = tickerEntry.Value;
                var tradeSignal = agentReport?.TradeSignal;

                if (tradeSignal == null)
                    continue;

                if (state.RiskAssessments.TryGetValue(tradeSignal.Ticker, out var risk))
                {
                    tradeSignal.SetRiskAssessment(risk);
                }
                else
                {
                    _logger.LogWarning($"No risk assessment found for ticker {tradeSignal.Ticker}");
                }
            }
        }
    }
}