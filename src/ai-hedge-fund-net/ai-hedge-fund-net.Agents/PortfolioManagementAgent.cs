using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class PortfolioManagementAgent : ITradingAgent
{
    public string Name { get; }

    public IDictionary<string, IDictionary<string, IEnumerable<string>>> AnalyzeEarningsStability()
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, FinancialStrength> AnalyzeFinancialStrength()
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, IEnumerable<string>> AnalyzeValuation()
    {
        throw new NotImplementedException();
    }

    public Task<TradeSignal> GenerateOutputAsync()
    {
        throw new NotImplementedException();
    }
}