namespace AiHedgeFund.Data.AlphaVantage;

public class CashFlow
{
    public string Symbol { get; set; }
    public List<CashFlowReport> AnnualReports { get; set; }
    public List<CashFlowReport> QuarterlyReports { get; set; }
}

public class CashFlowReport
{
    public DateTime FiscalDateEnding { get; set; }
    public string ReportedCurrency { get; set; }
    public decimal OperatingCashflow { get; set; }
    public decimal PaymentsForOperatingActivities { get; set; }
    public decimal ProceedsFromOperatingActivities { get; set; }
    public decimal ChangeInOperatingLiabilities { get; set; }
    public decimal ChangeInOperatingAssets { get; set; }
    public decimal DepreciationDepletionAndAmortization { get; set; }
    public decimal CapitalExpenditures { get; set; }
    public decimal ChangeInReceivables { get; set; }
    public decimal ChangeInInventory { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal CashflowFromInvestment { get; set; }
    public decimal CashflowFromFinancing { get; set; }
    public decimal ProceedsFromRepaymentsOfShortTermDebt { get; set; }
    public decimal PaymentsForRepurchaseOfCommonStock { get; set; }
    public decimal PaymentsForRepurchaseOfEquity { get; set; }
    public decimal PaymentsForRepurchaseOfPreferredStock { get; set; }
    public decimal? DividendPayout { get; set; }
    public decimal DividendPayoutCommonStock { get; set; }
    public decimal DividendPayoutPreferredStock { get; set; }
    public decimal ProceedsFromIssuanceOfCommonStock { get; set; }
    public decimal ProceedsFromIssuanceOfPreferredStock { get; set; }
    public decimal ProceedsFromRepurchaseOfEquity { get; set; }
    public decimal ProceedsFromSaleOfTreasuryStock { get; set; }
    public decimal ChangeInCashAndCashEquivalents { get; set; }
    public decimal ChangeInExchangeRate { get; set; }
    public decimal? NetIncome { get; set; }

    public decimal? OperatingCashFlow { get; set; }
    public decimal? FreeCashFlow { get; set; }

    public Dictionary<string, decimal?> GetLineItems()
    {
        return new Dictionary<string, decimal?>
        {
            ["OperatingCashFlow"] = OperatingCashFlow,
            ["CapitalExpenditures"] = CapitalExpenditures,
            ["FreeCashFlow"] = FreeCashFlow
        };
    }
}