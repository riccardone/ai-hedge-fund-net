using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class CharlieMunger : ITradingAgent
{
    public string Name => nameof(CharlieMunger);

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

    public TradeSignal GenerateOutput()
    {
        throw new NotImplementedException();
    }
}