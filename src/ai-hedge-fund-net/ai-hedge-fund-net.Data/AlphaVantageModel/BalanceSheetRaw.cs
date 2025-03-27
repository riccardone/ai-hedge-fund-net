namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class BalanceSheetRaw
{
    public string Symbol { get; set; }
    public List<BalanceSheetReportRaw> AnnualReports { get; set; }
    public List<BalanceSheetReportRaw> QuarterlyReports { get; set; }
}

public class BalanceSheetReportRaw
{
    public string FiscalDateEnding { get; set; }
    public string ReportedCurrency { get; set; }
    public string TotalAssets { get; set; }
    public string TotalCurrentAssets { get; set; }
    public string CashAndCashEquivalentsAtCarryingValue { get; set; }
    public string CashAndShortTermInvestments { get; set; }
    public string Inventory { get; set; }
    public string CurrentNetReceivables { get; set; }
    public string TotalNonCurrentAssets { get; set; }
    public string PropertyPlantEquipment { get; set; }
    public string AccumulatedDepreciationAmortizationPPE { get; set; }
    public string IntangibleAssets { get; set; }
    public string IntangibleAssetsExcludingGoodwill { get; set; }
    public string Goodwill { get; set; }
    public string Investments { get; set; }
    public string LongTermInvestments { get; set; }
    public string ShortTermInvestments { get; set; }
    public string OtherCurrentAssets { get; set; }
    public string OtherNonCurrentAssets { get; set; }
    public string TotalLiabilities { get; set; }
    public string TotalCurrentLiabilities { get; set; }
    public string CurrentAccountsPayable { get; set; }
    public string DeferredRevenue { get; set; }
    public string CurrentDebt { get; set; }
    public string ShortTermDebt { get; set; }
    public string TotalNonCurrentLiabilities { get; set; }
    public string CapitalLeaseObligations { get; set; }
    public string LongTermDebt { get; set; }
    public string CurrentLongTermDebt { get; set; }
    public string LongTermDebtNoncurrent { get; set; }
    public string ShortLongTermDebtTotal { get; set; }
    public string OtherCurrentLiabilities { get; set; }
    public string OtherNonCurrentLiabilities { get; set; }
    public string TotalShareholderEquity { get; set; }
    public string TreasuryStock { get; set; }
    public string RetainedEarnings { get; set; }
    public string CommonStock { get; set; }
    public string CommonStockSharesOutstanding { get; set; }
}