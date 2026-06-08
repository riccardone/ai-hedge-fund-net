using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents.Services;

public class PortfolioManager
{
    private readonly ILogger<PortfolioManager> _logger;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IBaseRateProvider? _baseRateProvider;

    public PortfolioManager(IAgentRegistry agentRegistry, ILogger<PortfolioManager> logger,
        IBaseRateProvider? baseRateProvider = null)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
        _baseRateProvider = baseRateProvider;
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
                News: news ?? Enumerable.Empty<NewsSentiment>(),
                BaseRate: TryGetBaseRate(ticker, prices, state)
            );

            var result = agent.Analyze(input);
            AppendBaseRateAdvisory(input.BaseRate, result);
            results.Add(result);
        }

        return results;
    }

    private BaseRate? TryGetBaseRate(string ticker, IEnumerable<Price>? prices, TradingWorkflowState state)
    {
        if (_baseRateProvider is not { Enabled: true })
            return null;

        // No-lookahead: anchor on the latest priced bar (the decision date), falling back
        // to the workflow end date. The historical cohort is drawn only from earlier bars.
        var asOf = prices?.MaxBy(p => p.Date)?.Date ?? state.EndDate;
        return _baseRateProvider.TryGetBaseRate(ticker, asOf, out var baseRate) ? baseRate : null;
    }

    private static void AppendBaseRateAdvisory(BaseRate? baseRate, AgentResult result)
    {
        if (baseRate is null || result.Signal is null)
            return;

        // Advisory only: flag when the signal runs counter to a clearly-signed base rate.
        // The signal itself is never altered.
        var note = baseRate.AssessConflict(result.Signal.Signal);
        if (string.IsNullOrEmpty(note))
            return;

        var reasoning = result.Signal.Reasoning;
        result.Signal.Reasoning = string.IsNullOrWhiteSpace(reasoning) ? note : $"{reasoning}\n\n{note}";
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