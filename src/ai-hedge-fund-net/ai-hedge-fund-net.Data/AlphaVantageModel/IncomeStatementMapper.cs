namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class IncomeStatementMapper
{
    public static IncomeStatement Map(IncomeStatementRaw raw)
    {
        return new IncomeStatement
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports.Select(MapReport).ToList(),
            QuarterlyReports = raw.QuarterlyReports.Select(MapReport).ToList()
        };
    }

    private static IncomeStatementReport MapReport(Dictionary<string, string> rawOriginal)
    {
        // Normalize the dictionary to be case-insensitive
        var raw = new Dictionary<string, string>(rawOriginal, StringComparer.OrdinalIgnoreCase);

        return new IncomeStatementReport
        {
            FiscalDateEnding = ParseDate(raw.GetValueOrDefault("fiscalDateEnding")),
            ReportedCurrency = raw.GetValueOrDefault("reportedCurrency"),

            GrossProfit = ParseNullableDecimal(raw.GetValueOrDefault("grossProfit")),
            TotalRevenue = ParseNullableDecimal(raw.GetValueOrDefault("totalRevenue")),
            CostOfRevenue = ParseNullableDecimal(raw.GetValueOrDefault("costOfRevenue")),
            CostofGoodsAndServicesSold = ParseNullableDecimal(raw.GetValueOrDefault("costofGoodsAndServicesSold")),
            OperatingIncome = ParseNullableDecimal(raw.GetValueOrDefault("operatingIncome")),
            SellingGeneralAndAdministrative = ParseNullableDecimal(raw.GetValueOrDefault("sellingGeneralAndAdministrative")),
            ResearchAndDevelopment = ParseNullableDecimal(raw.GetValueOrDefault("researchAndDevelopment")),
            OperatingExpenses = ParseNullableDecimal(raw.GetValueOrDefault("operatingExpenses")),
            InvestmentIncomeNet = ParseNullableDecimal(raw.GetValueOrDefault("investmentIncomeNet")),
            NetInterestIncome = ParseNullableDecimal(raw.GetValueOrDefault("netInterestIncome")),
            InterestIncome = ParseNullableDecimal(raw.GetValueOrDefault("interestIncome")),
            InterestExpense = ParseNullableDecimal(raw.GetValueOrDefault("interestExpense")),
            NonInterestIncome = ParseNullableDecimal(raw.GetValueOrDefault("nonInterestIncome")),
            OtherNonOperatingIncome = ParseNullableDecimal(raw.GetValueOrDefault("otherNonOperatingIncome")),
            Depreciation = ParseNullableDecimal(raw.GetValueOrDefault("depreciation")),
            DepreciationAndAmortization = ParseNullableDecimal(raw.GetValueOrDefault("depreciationAndAmortization")),
            IncomeBeforeTax = ParseNullableDecimal(raw.GetValueOrDefault("incomeBeforeTax")),
            IncomeTaxExpense = ParseNullableDecimal(raw.GetValueOrDefault("incomeTaxExpense")),
            InterestAndDebtExpense = ParseNullableDecimal(raw.GetValueOrDefault("interestAndDebtExpense")),
            NetIncomeFromContinuingOperations = ParseNullableDecimal(raw.GetValueOrDefault("netIncomeFromContinuingOperations")),
            ComprehensiveIncomeNetOfTax = ParseNullableDecimal(raw.GetValueOrDefault("comprehensiveIncomeNetOfTax")),
            Ebit = ParseNullableDecimal(raw.GetValueOrDefault("ebit")),
            Ebitda = ParseNullableDecimal(raw.GetValueOrDefault("ebitda")),
            NetIncome = ParseNullableDecimal(raw.GetValueOrDefault("netIncome"))
        };
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : null;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : default;
    }
}
