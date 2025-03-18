using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class WarrenBuffett : ITradingAgent
{
    public string Name => nameof(WarrenBuffett);

    public IDictionary<string, IDictionary<string, IEnumerable<string>>> AnalyzeEarningsStability()
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, IEnumerable<string>> AnalyzeFinancialStrength()
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