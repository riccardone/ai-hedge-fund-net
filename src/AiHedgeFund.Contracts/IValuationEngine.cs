using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public interface IValuationEngine
{
    bool TryCalculateIntrinsicValue(FinancialMetrics latest, RiskLevel riskLevel, decimal? currentPrice,
        out ValuationSummary? result);
}