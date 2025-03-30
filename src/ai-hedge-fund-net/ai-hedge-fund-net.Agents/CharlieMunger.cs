using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class CharlieMunger : ITradingAgent
{
    public string Name { get; }
    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker)
    {
        throw new NotImplementedException();
    }

    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker)
    {
        throw new NotImplementedException();
    }

    public FinancialAnalysisResult AnalyzeValuation(string ticker)
    {
        throw new NotImplementedException();
    }

    public TradeSignal GenerateOutput(string ticker)
    {
        throw new NotImplementedException();
    }
}