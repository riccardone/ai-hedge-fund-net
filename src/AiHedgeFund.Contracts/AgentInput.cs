using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public record AgentInput(
    string Ticker,
    string Exchange,
    RiskLevel RiskLevel,
    string Model,
    IEnumerable<FinancialMetrics> Metrics,
    IEnumerable<FinancialLineItem> LineItems,
    IEnumerable<Price> Prices,
    IEnumerable<NewsSentiment> News
);
