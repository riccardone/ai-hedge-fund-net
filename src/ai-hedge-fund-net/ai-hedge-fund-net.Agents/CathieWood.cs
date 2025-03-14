using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents
{
    public class CathieWood : ITradingAgent
    {
        public IDictionary<string, IEnumerable<string>> AnalyzeEarningsStability(IEnumerable<FinancialMetrics> financialMetricsItems, IEnumerable<FinancialLineItem> financialLineItems)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IEnumerable<string>> AnalyzeFinancialStrength(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IEnumerable<string>> AnalyzeValuation(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems, decimal marketCap)
        {
            throw new NotImplementedException();
        }

        public TradeSignal GenerateOutput()
        {
            throw new NotImplementedException();
        }
    }
}
