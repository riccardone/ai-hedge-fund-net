using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public record AgentResult(
    string AgentKey,
    string AgentDisplayName,
    string Ticker,
    TradeSignal Signal,
    IReadOnlyList<FinancialAnalysisResult> Scores
)
{
    public static AgentResult Failure(string agentKey, string displayName, string ticker) =>
        new(agentKey, displayName, ticker,
            new TradeSignal(ticker, "neutral", 0, "Agent failed to produce a result."),
            Array.Empty<FinancialAnalysisResult>());
}
