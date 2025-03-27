namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class CashFlowMapper
{
    public static CashFlow Map(CashFlowRaw raw)
    {
        return new CashFlow
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports.Select(MapReport).ToList(),
            QuarterlyReports = raw.QuarterlyReports.Select(MapReport).ToList()
        };
    }

    private static CashFlowReport MapReport(Dictionary<string, string> raw)
    {
        return new CashFlowReport
        {
            FiscalDateEnding = ParseDate(raw.GetValueOrDefault("fiscalDateEnding")),
            ReportedCurrency = raw.GetValueOrDefault("reportedCurrency"),
            OperatingCashFlow = ParseNullableDecimal(raw.GetValueOrDefault("operatingCashflow")),
            PaymentsForOperatingActivities = ParseNullableDecimal(raw.GetValueOrDefault("paymentsForOperatingActivities")),
            ProceedsFromOperatingActivities = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromOperatingActivities")),
            ChangeInOperatingLiabilities = ParseNullableDecimal(raw.GetValueOrDefault("changeInOperatingLiabilities")),
            ChangeInOperatingAssets = ParseNullableDecimal(raw.GetValueOrDefault("changeInOperatingAssets")),
            DepreciationDepletionAndAmortization = ParseNullableDecimal(raw.GetValueOrDefault("depreciationDepletionAndAmortization")),
            CapitalExpenditures = ParseNullableDecimal(raw.GetValueOrDefault("capitalExpenditures")),
            ChangeInReceivables = ParseNullableDecimal(raw.GetValueOrDefault("changeInReceivables")),
            ChangeInInventory = ParseNullableDecimal(raw.GetValueOrDefault("changeInInventory")),
            ProfitLoss = ParseNullableDecimal(raw.GetValueOrDefault("profitLoss")),
            CashFlowFromInvestment = ParseNullableDecimal(raw.GetValueOrDefault("cashflowFromInvestment")),
            CashFlowFromFinancing = ParseNullableDecimal(raw.GetValueOrDefault("cashflowFromFinancing")),
            ProceedsFromRepaymentsOfShortTermDebt = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromRepaymentsOfShortTermDebt")),
            PaymentsForRepurchaseOfCommonStock = ParseNullableDecimal(raw.GetValueOrDefault("paymentsForRepurchaseOfCommonStock")),
            PaymentsForRepurchaseOfEquity = ParseNullableDecimal(raw.GetValueOrDefault("paymentsForRepurchaseOfEquity")),
            PaymentsForRepurchaseOfPreferredStock = ParseNullableDecimal(raw.GetValueOrDefault("paymentsForRepurchaseOfPreferredStock")),
            DividendPayout = ParseNullableDecimal(raw.GetValueOrDefault("dividendPayout")),
            DividendPayoutCommonStock = ParseNullableDecimal(raw.GetValueOrDefault("dividendPayoutCommonStock")),
            DividendPayoutPreferredStock = ParseNullableDecimal(raw.GetValueOrDefault("dividendPayoutPreferredStock")),
            ProceedsFromIssuanceOfCommonStock = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromIssuanceOfCommonStock")),
            ProceedsFromIssuanceOfLongTermDebtAndCapitalSecuritiesNet = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromIssuanceOfLongTermDebtAndCapitalSecuritiesNet")),
            ProceedsFromIssuanceOfPreferredStock = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromIssuanceOfPreferredStock")),
            ProceedsFromRepurchaseOfEquity = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromRepurchaseOfEquity")),
            ProceedsFromSaleOfTreasuryStock = ParseNullableDecimal(raw.GetValueOrDefault("proceedsFromSaleOfTreasuryStock")),
            ChangeInCashAndCashEquivalents = ParseNullableDecimal(raw.GetValueOrDefault("changeInCashAndCashEquivalents")),
            ChangeInExchangeRate = ParseNullableDecimal(raw.GetValueOrDefault("changeInExchangeRate")),
            NetIncome = ParseNullableDecimal(raw.GetValueOrDefault("netIncome"))
        };
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : (decimal?)null;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : default;
    }
}