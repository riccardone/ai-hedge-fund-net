using System.Runtime.InteropServices.ComTypes;

namespace AiHedgeFund.Contracts.Model;

public class FinancialMetrics
{
    public string Ticker { get; set; }
    public string ReportPeriod { get; set; }
    public string Period { get; set; }
    public string Currency { get; set; } 
    public decimal? MarketCap { get; set; }
    public decimal? EnterpriseValue { get; set; }
    public decimal? PriceToEarningsRatio { get; set; }
    public decimal? PriceToBookRatio { get; set; }
    public decimal? PriceToSalesRatio { get; set; }
    public decimal? EnterpriseValueToEbitdaRatio { get; set; }
    public decimal? EnterpriseValueToRevenueRatio { get; set; }
    public decimal? FreeCashFlowYield { get; set; }
    public decimal? PegRatio { get; set; }
    public decimal? GrossMargin { get; set; }
    public decimal? OperatingMargin { get; set; }
    public decimal? NetMargin { get; set; }
    public decimal? ReturnOnEquity { get; set; }
    public decimal? ReturnOnAssets { get; set; }
    public decimal? ReturnOnInvestedCapital { get; set; }
    public decimal? AssetTurnover { get; set; }
    public decimal? InventoryTurnover { get; set; }
    public decimal? ReceivablesTurnover { get; set; }
    public decimal? DaysSalesOutstanding { get; set; }
    public decimal? OperatingCycle { get; set; }
    public decimal? WorkingCapitalTurnover { get; set; }
    public decimal? CurrentRatio { get; set; }
    public decimal? QuickRatio { get; set; }
    public decimal? CashRatio { get; set; }
    public decimal? OperatingCashFlowRatio { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? DebtToAssets { get; set; }
    public decimal? InterestCoverage { get; set; }
    public decimal? RevenueGrowth { get; set; }
    public decimal? EarningsGrowth { get; set; }
    public decimal? BookValueGrowth { get; set; }
    public decimal? EarningsPerShareGrowth { get; set; }
    public decimal? FreeCashFlowGrowth { get; set; }
    public decimal? OperatingIncomeGrowth { get; set; }
    public decimal? EbitdaGrowth { get; set; }
    public decimal? PayoutRatio { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? BookValuePerShare { get; set; }
    public decimal? FreeCashFlowPerShare { get; set; }
    public decimal OperatingCashFlow { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal CapitalExpenditure { get; set; }
    public decimal DividendsAndOtherCashDistributions { get; set; }
    public decimal FreeCashFlow => OperatingCashFlow - CapitalExpenditure;
    public decimal? CommonStockSharesOutstanding { get; set; }
    public decimal? GoodwillAndIntangibleAssets { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? TotalShareholderEquity { get; set; }
    public decimal? CashAndCashEquivalentsAtCarryingValue { get; set; }
    public decimal? TransactionSharesFromInsiders { get; set; }
}