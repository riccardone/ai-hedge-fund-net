using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface ITradingAgent
{
    IDictionary<string, IEnumerable<string>> AnalyzeEarningsStability(IEnumerable<FinancialMetrics> financialMetricsItems, IEnumerable<FinancialLineItem> financialLineItems);
    IDictionary<string, IEnumerable<string>> AnalyzeFinancialStrength(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems);
    IDictionary<string, IEnumerable<string>> AnalyzeValuation(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems, decimal marketCap);
    TradeSignal GenerateOutput();
}