namespace AiHedgeFund.Contracts.Model;

public class AgentReport
{
    public AgentReport(string agentName, TradeSignal tradeSignal, decimal confidence, string reasoning, IEnumerable<FinancialAnalysisResult> financialAnalysisResults)
    {
        AgentName = agentName;
        TradeSignal = tradeSignal;
        Confidence = confidence;
        Reasoning = reasoning;
        FinancialAnalysisResults = financialAnalysisResults;
    }

    public string AgentName { get; }
    public IEnumerable<FinancialAnalysisResult> FinancialAnalysisResults { get; } 
    public TradeSignal TradeSignal { get; }
    public decimal Confidence { get; }
    public string Reasoning { get; }
}