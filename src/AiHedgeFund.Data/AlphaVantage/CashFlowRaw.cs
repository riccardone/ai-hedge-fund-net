namespace AiHedgeFund.Data.AlphaVantage;

public class CashFlowRaw
{
    public string Symbol { get; set; }
    public List<CashFlowReportRaw> AnnualReports { get; set; }
    public List<CashFlowReportRaw> QuarterlyReports { get; set; }
}

public class CashFlowReportRaw
{
    public string FiscalDateEnding { get; set; }
    public string ReportedCurrency { get; set; }
    public string OperatingCashflow { get; set; }
    public string PaymentsForOperatingActivities { get; set; }
    public string ProceedsFromOperatingActivities { get; set; }
    public string ChangeInOperatingLiabilities { get; set; }
    public string ChangeInOperatingAssets { get; set; }
    public string DepreciationDepletionAndAmortization { get; set; }
    public string CapitalExpenditures { get; set; }
    public string ChangeInReceivables { get; set; }
    public string ChangeInInventory { get; set; }
    public string ProfitLoss { get; set; }
    public string CashflowFromInvestment { get; set; }
    public string CashflowFromFinancing { get; set; }
    public string ProceedsFromRepaymentsOfShortTermDebt { get; set; }
    public string PaymentsForRepurchaseOfCommonStock { get; set; }
    public string PaymentsForRepurchaseOfEquity { get; set; }
    public string PaymentsForRepurchaseOfPreferredStock { get; set; }
    public string DividendPayout { get; set; }
    public string DividendPayoutCommonStock { get; set; }
    public string DividendPayoutPreferredStock { get; set; }
    public string ProceedsFromIssuanceOfCommonStock { get; set; }
    public string ProceedsFromIssuanceOfPreferredStock { get; set; }
    public string ProceedsFromRepurchaseOfEquity { get; set; }
    public string ProceedsFromSaleOfTreasuryStock { get; set; }
    public string ChangeInCashAndCashEquivalents { get; set; }
    public string ChangeInExchangeRate { get; set; }
    public string NetIncome { get; set; }
}