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

    public IReadOnlyList<AgentResult> Evaluate(string agentKey, TradingWorkflowState state)
    {
        if (!_agentRegistry.TryGet(agentKey, out var agent) || agent is null)
        {
            _logger.LogWarning("Agent '{AgentKey}' not found in registry", agentKey);
            return Array.Empty<AgentResult>();
        }

        var results = new List<AgentResult>();
        foreach (var ticker in state.Tickers)
        {
            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics) ||
                !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                _logger.LogWarning("Missing financial data for {Ticker} — skipping {AgentKey}", ticker, agentKey);
                continue;
            }

            state.Prices.TryGetValue(ticker, out var prices);
            state.CompanyNews.TryGetValue(ticker, out var news);

            var input = new AgentInput(
                Ticker: ticker,
                Exchange: string.Empty,
                RiskLevel: state.RiskLevel,
                Model: state.ModelName,
                Metrics: metrics,
                LineItems: lineItems,
                Prices: prices ?? Enumerable.Empty<Price>(),
                News: news ?? Enumerable.Empty<NewsSentiment>()
            );

            results.Add(agent.Analyze(input));
        }

        return results;
    }

    public void RunRiskAssessments(TradingWorkflowState state, RiskManagerAgent riskAgent,
        IReadOnlyList<AgentResult> agentResults)
    {
        riskAgent.Run(state);

        foreach (var result in agentResults)
        {
            if (state.RiskAssessments.TryGetValue(result.Ticker, out var risk))
                result.Signal.SetRiskAssessment(risk);
            else
                _logger.LogWarning("No risk assessment found for ticker {Ticker}", result.Ticker);
        }
    }
}