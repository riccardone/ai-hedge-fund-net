namespace AiHedgeFund.Data.AlphaVantage;

public static class CashFlowMapper
{
    public static CashFlow Map(CashFlowRaw raw)
    {
        return new CashFlow
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports?.Select(MapReport).ToList() ?? new(),
            QuarterlyReports = raw.QuarterlyReports?.Select(MapReport).ToList() ?? new()
        };
    }

    private static CashFlowReport MapReport(CashFlowReportRaw raw)
    {
        return new CashFlowReport
        {
            FiscalDateEnding = DateTime.TryParse(raw.FiscalDateEnding, out var date) ? date : default,
            ReportedCurrency = raw.ReportedCurrency,
            OperatingCashflow = ParseDecimal(raw.OperatingCashflow),
            PaymentsForOperatingActivities = ParseDecimal(raw.PaymentsForOperatingActivities),
            ProceedsFromOperatingActivities = ParseDecimal(raw.ProceedsFromOperatingActivities),
            ChangeInOperatingLiabilities = ParseDecimal(raw.ChangeInOperatingLiabilities),
            ChangeInOperatingAssets = ParseDecimal(raw.ChangeInOperatingAssets),
            DepreciationDepletionAndAmortization = ParseDecimal(raw.DepreciationDepletionAndAmortization),
            CapitalExpenditures = ParseDecimal(raw.CapitalExpenditures),
            ChangeInReceivables = ParseDecimal(raw.ChangeInReceivables),
            ChangeInInventory = ParseDecimal(raw.ChangeInInventory),
            ProfitLoss = ParseDecimal(raw.ProfitLoss),
            CashflowFromInvestment = ParseDecimal(raw.CashflowFromInvestment),
            CashflowFromFinancing = ParseDecimal(raw.CashflowFromFinancing),
            ProceedsFromRepaymentsOfShortTermDebt = ParseDecimal(raw.ProceedsFromRepaymentsOfShortTermDebt),
            PaymentsForRepurchaseOfCommonStock = ParseDecimal(raw.PaymentsForRepurchaseOfCommonStock),
            PaymentsForRepurchaseOfEquity = ParseDecimal(raw.PaymentsForRepurchaseOfEquity),
            PaymentsForRepurchaseOfPreferredStock = ParseDecimal(raw.PaymentsForRepurchaseOfPreferredStock),
            DividendPayout = ParseDecimal(raw.DividendPayout),
            DividendPayoutCommonStock = ParseDecimal(raw.DividendPayoutCommonStock),
            DividendPayoutPreferredStock = ParseDecimal(raw.DividendPayoutPreferredStock),
            ProceedsFromIssuanceOfCommonStock = ParseDecimal(raw.ProceedsFromIssuanceOfCommonStock),
            ProceedsFromIssuanceOfPreferredStock = ParseDecimal(raw.ProceedsFromIssuanceOfPreferredStock),
            ProceedsFromRepurchaseOfEquity = ParseDecimal(raw.ProceedsFromRepurchaseOfEquity),
            ProceedsFromSaleOfTreasuryStock = ParseDecimal(raw.ProceedsFromSaleOfTreasuryStock),
            ChangeInCashAndCashEquivalents = ParseDecimal(raw.ChangeInCashAndCashEquivalents),
            ChangeInExchangeRate = ParseDecimal(raw.ChangeInExchangeRate),
            NetIncome = ParseDecimal(raw.NetIncome),
        };
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0;
    }
}