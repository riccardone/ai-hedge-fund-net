namespace ai_hedge_fund_net.Contracts.Model;

public class FinancialMetrics
{
    public string Ticker { get; set; }
    public string ReportPeriod { get; set; }
    public string Period { get; set; }
    public string Currency { get; set; }    

    public double? MarketCap { get; set; }
    public double? EnterpriseValue { get; set; }
    public double? PriceToEarningsRatio { get; set; }
    public double? PriceToBookRatio { get; set; }
    public double? PriceToSalesRatio { get; set; }
    public double? EnterpriseValueToEbitdaRatio { get; set; }
    public double? EnterpriseValueToRevenueRatio { get; set; }
    public double? FreeCashFlowYield { get; set; }
    public double? PegRatio { get; set; }
    public double? GrossMargin { get; set; }
    public double? OperatingMargin { get; set; }
    public double? NetMargin { get; set; }
    public double? ReturnOnEquity { get; set; }
    public double? ReturnOnAssets { get; set; }
    public double? ReturnOnInvestedCapital { get; set; }
    public double? AssetTurnover { get; set; }
    public double? InventoryTurnover { get; set; }
    public double? ReceivablesTurnover { get; set; }
    public double? DaysSalesOutstanding { get; set; }
    public double? OperatingCycle { get; set; }
    public double? WorkingCapitalTurnover { get; set; }
    public double? CurrentRatio { get; set; }
    public double? QuickRatio { get; set; }
    public double? CashRatio { get; set; }
    public double? OperatingCashFlowRatio { get; set; }
    public double? DebtToEquity { get; set; }
    public double? DebtToAssets { get; set; }
    public double? InterestCoverage { get; set; }
    public double? RevenueGrowth { get; set; }
    public double? EarningsGrowth { get; set; }
    public double? BookValueGrowth { get; set; }
    public double? EarningsPerShareGrowth { get; set; }
    public double? FreeCashFlowGrowth { get; set; }
    public double? OperatingIncomeGrowth { get; set; }
    public double? EbitdaGrowth { get; set; }
    public double? PayoutRatio { get; set; }
    public double? EarningsPerShare { get; set; }
    public double? BookValuePerShare { get; set; }
    public double? FreeCashFlowPerShare { get; set; }
    public TradeSignal Analysis { get; set; }
}