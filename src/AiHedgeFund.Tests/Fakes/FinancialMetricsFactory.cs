using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Tests.Fakes;

public static class FinancialMetricsFactory
{
    public static FinancialMetrics CreateTestMetrics(
        string ticker,
        int year,
        decimal netIncome,
        decimal operatingCashFlow,
        decimal capitalExpenditure,
        decimal depreciation,
        decimal marketCap,
        decimal? outstandingShares = null)
    {
        return new FinancialMetrics
        {
            Ticker = ticker,
            Period = year.ToString(),
            ReportPeriod = new DateTime(year, 12, 31).ToString("O"),
            Currency = "USD",
            NetIncome = netIncome,
            OperatingCashFlow = operatingCashFlow,
            CapitalExpenditure = capitalExpenditure,
            DepreciationAndAmortization = depreciation,
            MarketCap = marketCap,
            OutstandingShares = outstandingShares ?? 10000m,
            TotalRevenue = operatingCashFlow + 10000m, // mock revenue
            OperatingMargin = 0.30m,
            ReturnOnEquity = 0.25m,
            CurrentRatio = 1.5m,
            DebtToEquity = 1.1m,
            GrossMargin = 0.40m,
            ReturnOnInvestedCapital = 0.18m,
            EarningsPerShare = netIncome / (outstandingShares ?? 10000m),
            FreeCashFlowPerShare = (operatingCashFlow - capitalExpenditure) / (outstandingShares ?? 10000m),
            FreeCashFlowYield = (operatingCashFlow - capitalExpenditure) / marketCap
        };
    }
}