using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents.Services;

public class DefaultValuationEngine : IValuationEngine
{
    private readonly ILogger<DefaultValuationEngine> _logger;

    public DefaultValuationEngine(ILogger<DefaultValuationEngine> logger)
    {
        _logger = logger;
    }

    private static decimal Pow(decimal baseValue, int exponent)
    {
        if (exponent == 0) return 1;
        if (exponent < 0) throw new ArgumentOutOfRangeException(nameof(exponent));

        decimal result = 1;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1)
                result *= baseValue;

            baseValue *= baseValue;
            exponent >>= 1;
        }

        return result;
    }

    public bool TryCalculateIntrinsicValue(FinancialMetrics latest, RiskLevel riskLevel, decimal? currentPrice, out ValuationSummary? result)
    {
        result = null;
        if (latest == null)
        {
            _logger?.LogWarning("Valuation basis could not be determined (the latest metric object is null)");
            return false;
        }

        if (latest.OutstandingShares == null)
        {
            _logger?.LogWarning("Valuation basis could not be determined (missing OutstandingShares)");
            return false;
        }

        if (latest.OutstandingShares == 0)
        {
            _logger?.LogWarning("Valuation basis could not be determined (OutstandingShares is 0)");
            return false;
        }

        decimal? valuationBasis = null;
        string basisLabel = "Unknown";

        switch (riskLevel)
        {
            case RiskLevel.Low:
                var netIncome = latest.NetIncome;
                var depreciation = latest.DepreciationAndAmortization;
                var capex = latest.CapitalExpenditure;
                if (netIncome.HasValue && depreciation.HasValue && capex.HasValue)
                {
                    var maintenanceCapEx = capex.Value * 0.75m;
                    valuationBasis = netIncome.Value + depreciation.Value - maintenanceCapEx;
                    basisLabel = "Owner Earnings";
                }
                break;

            case RiskLevel.Medium:
            case RiskLevel.High:
                if (latest.OperatingCashFlow.HasValue && latest.CapitalExpenditure.HasValue)
                {
                    valuationBasis = latest.OperatingCashFlow.Value - latest.CapitalExpenditure.Value;
                    basisLabel = "Free Cash Flow";
                }
                break;
        }

        if (!valuationBasis.HasValue)
        {
            _logger?.LogWarning("Valuation basis could not be determined (missing required metrics)");
            return false;
        }

        // Risk-based assumptions
        decimal growthRate, discountRate;
        int terminalMultiple;

        switch (riskLevel)
        {
            case RiskLevel.Low:
                growthRate = 0.05m;
                discountRate = 0.09m;
                terminalMultiple = 12;
                break;
            case RiskLevel.Medium:
                growthRate = 0.08m;
                discountRate = 0.07m;
                terminalMultiple = 16;
                break;
            case RiskLevel.High:
                growthRate = 0.12m;
                discountRate = 0.06m;
                terminalMultiple = 20;
                break;
            default:
                return false;
        }

        const int projectionYears = 10;
        decimal futureValue = 0;

        for (int year = 1; year <= projectionYears; year++)
        {
            var future = valuationBasis.Value * Pow(1 + growthRate, year);
            var present = future / Pow(1 + discountRate, year);
            futureValue += present;
        }

        var terminalValue = (valuationBasis.Value * Pow(1 + growthRate, projectionYears) * terminalMultiple)
                            / Pow(1 + discountRate, projectionYears);

        var intrinsicValueTotal = futureValue + terminalValue;
        var intrinsicValuePerShare = intrinsicValueTotal / latest.OutstandingShares.Value;

        decimal? marginOfSafety = null;
        if (currentPrice.HasValue && currentPrice.Value > 0)
            marginOfSafety = (intrinsicValuePerShare - currentPrice.Value) / currentPrice.Value;

        result = new ValuationSummary
        {
            IntrinsicValue = intrinsicValuePerShare,
            MarginOfSafety = marginOfSafety ?? 0,
            ValuationBasis = valuationBasis.Value,
            ValuationBasisLabel = basisLabel,
            RiskLevel = riskLevel,
            GrowthRate = growthRate,
            DiscountRate = discountRate,
            TerminalMultiple = terminalMultiple
        };
        return true;
    }
}