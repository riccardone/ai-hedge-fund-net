namespace ai_hedge_fund_net.Contracts.Model;

public class FinancialMetrics
{
    public string Ticker { get; }
    public string ReportPeriod { get; }
    public string Period { get; }
    public string Currency { get; }

    public double? MarketCap { get; }
    public double? EnterpriseValue { get; }
    public double? PriceToEarningsRatio { get; }
    public double? PriceToBookRatio { get; }
    public double? PriceToSalesRatio { get; }
    public double? EnterpriseValueToEbitdaRatio { get; }
    public double? EnterpriseValueToRevenueRatio { get; }
    public double? FreeCashFlowYield { get; }
    public double? PegRatio { get; }
    public double? GrossMargin { get; }
    public double? OperatingMargin { get; }
    public double? NetMargin { get; }
    public double? ReturnOnEquity { get; }
    public double? ReturnOnAssets { get; }
    public double? ReturnOnInvestedCapital { get; }
    public double? AssetTurnover { get; }
    public double? InventoryTurnover { get; }
    public double? ReceivablesTurnover { get; }
    public double? DaysSalesOutstanding { get; }
    public double? OperatingCycle { get; }
    public double? WorkingCapitalTurnover { get; }
    public double? CurrentRatio { get; }
    public double? QuickRatio { get; }
    public double? CashRatio { get; }
    public double? OperatingCashFlowRatio { get; }
    public double? DebtToEquity { get; }
    public double? DebtToAssets { get; }
    public double? InterestCoverage { get; }
    public double? RevenueGrowth { get; }
    public double? EarningsGrowth { get; }
    public double? BookValueGrowth { get; }
    public double? EarningsPerShareGrowth { get; }
    public double? FreeCashFlowGrowth { get; }
    public double? OperatingIncomeGrowth { get; }
    public double? EbitdaGrowth { get; }
    public double? PayoutRatio { get; }
    public double? EarningsPerShare { get; }
    public double? BookValuePerShare { get; }
    public double? FreeCashFlowPerShare { get; }

    public FinancialMetrics(
        string ticker,
        string reportPeriod,
        string period,
        string currency,
        double? marketCap,
        double? enterpriseValue,
        double? priceToEarningsRatio,
        double? priceToBookRatio,
        double? priceToSalesRatio,
        double? enterpriseValueToEbitdaRatio,
        double? enterpriseValueToRevenueRatio,
        double? freeCashFlowYield,
        double? pegRatio,
        double? grossMargin,
        double? operatingMargin,
        double? netMargin,
        double? returnOnEquity,
        double? returnOnAssets,
        double? returnOnInvestedCapital,
        double? assetTurnover,
        double? inventoryTurnover,
        double? receivablesTurnover,
        double? daysSalesOutstanding,
        double? operatingCycle,
        double? workingCapitalTurnover,
        double? currentRatio,
        double? quickRatio,
        double? cashRatio,
        double? operatingCashFlowRatio,
        double? debtToEquity,
        double? debtToAssets,
        double? interestCoverage,
        double? revenueGrowth,
        double? earningsGrowth,
        double? bookValueGrowth,
        double? earningsPerShareGrowth,
        double? freeCashFlowGrowth,
        double? operatingIncomeGrowth,
        double? ebitdaGrowth,
        double? payoutRatio,
        double? earningsPerShare,
        double? bookValuePerShare,
        double? freeCashFlowPerShare)
    {
        Ticker = ticker;
        ReportPeriod = reportPeriod;
        Period = period;
        Currency = currency;
        MarketCap = marketCap;
        EnterpriseValue = enterpriseValue;
        PriceToEarningsRatio = priceToEarningsRatio;
        PriceToBookRatio = priceToBookRatio;
        PriceToSalesRatio = priceToSalesRatio;
        EnterpriseValueToEbitdaRatio = enterpriseValueToEbitdaRatio;
        EnterpriseValueToRevenueRatio = enterpriseValueToRevenueRatio;
        FreeCashFlowYield = freeCashFlowYield;
        PegRatio = pegRatio;
        GrossMargin = grossMargin;
        OperatingMargin = operatingMargin;
        NetMargin = netMargin;
        ReturnOnEquity = returnOnEquity;
        ReturnOnAssets = returnOnAssets;
        ReturnOnInvestedCapital = returnOnInvestedCapital;
        AssetTurnover = assetTurnover;
        InventoryTurnover = inventoryTurnover;
        ReceivablesTurnover = receivablesTurnover;
        DaysSalesOutstanding = daysSalesOutstanding;
        OperatingCycle = operatingCycle;
        WorkingCapitalTurnover = workingCapitalTurnover;
        CurrentRatio = currentRatio;
        QuickRatio = quickRatio;
        CashRatio = cashRatio;
        OperatingCashFlowRatio = operatingCashFlowRatio;
        DebtToEquity = debtToEquity;
        DebtToAssets = debtToAssets;
        InterestCoverage = interestCoverage;
        RevenueGrowth = revenueGrowth;
        EarningsGrowth = earningsGrowth;
        BookValueGrowth = bookValueGrowth;
        EarningsPerShareGrowth = earningsPerShareGrowth;
        FreeCashFlowGrowth = freeCashFlowGrowth;
        OperatingIncomeGrowth = operatingIncomeGrowth;
        EbitdaGrowth = ebitdaGrowth;
        PayoutRatio = payoutRatio;
        EarningsPerShare = earningsPerShare;
        BookValuePerShare = bookValuePerShare;
        FreeCashFlowPerShare = freeCashFlowPerShare;
    }
}