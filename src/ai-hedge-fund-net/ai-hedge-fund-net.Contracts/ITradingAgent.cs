using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface ITradingAgent
{
    string Name { get; }
    FinancialAnalysisResult AnalyzeEarningsStability(string ticker);
    FinancialAnalysisResult AnalyzeFinancialStrength(string ticker);
    FinancialAnalysisResult AnalyzeValuation(string ticker);
    TradeSignal GenerateOutput(string ticker);
}