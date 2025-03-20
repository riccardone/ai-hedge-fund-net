using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface ITradingAgent
{
    string Name { get; }
    IDictionary<string, IDictionary<string, IEnumerable<string>>> AnalyzeEarningsStability();
    IDictionary<string, FinancialStrength> AnalyzeFinancialStrength();
    IDictionary<string, IEnumerable<string>> AnalyzeValuation();
    Task<TradeSignal> GenerateOutputAsync();
}