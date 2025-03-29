namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class IncomeStatementMapper
{
    public static IncomeStatement Map(IncomeStatementRaw raw)
    {
        return new IncomeStatement
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports?.Select(MapReport).ToList() ?? new(),
            QuarterlyReports = raw.QuarterlyReports?.Select(MapReport).ToList() ?? new()
        };
    }

    private static IncomeStatementReport MapReport(IncomeStatementReportRaw raw)
    {
        return new IncomeStatementReport
        {
            FiscalDateEnding = DateTime.TryParse(raw.FiscalDateEnding, out var date) ? date : default,
            ReportedCurrency = raw.ReportedCurrency,
            GrossProfit = ParseDecimal(raw.GrossProfit),
            TotalRevenue = ParseDecimal(raw.TotalRevenue),
            CostOfRevenue = ParseDecimal(raw.CostOfRevenue),
            CostOfGoodsAndServicesSold = ParseDecimal(raw.CostOfGoodsAndServicesSold),
            OperatingIncome = ParseDecimal(raw.OperatingIncome),
            SellingGeneralAndAdministrative = ParseDecimal(raw.SellingGeneralAndAdministrative),
            ResearchAndDevelopment = ParseDecimal(raw.ResearchAndDevelopment),
            OperatingExpenses = ParseDecimal(raw.OperatingExpenses),
            InvestmentIncomeNet = ParseDecimal(raw.InvestmentIncomeNet),
            NetInterestIncome = ParseDecimal(raw.NetInterestIncome),
            InterestIncome = ParseDecimal(raw.InterestIncome),
            InterestExpense = ParseDecimal(raw.InterestExpense),
            NonInterestIncome = ParseDecimal(raw.NonInterestIncome),
            OtherNonOperatingIncome = ParseDecimal(raw.OtherNonOperatingIncome),
            Depreciation = ParseDecimal(raw.Depreciation),
            DepreciationAndAmortization = ParseDecimal(raw.DepreciationAndAmortization),
            IncomeBeforeTax = ParseDecimal(raw.IncomeBeforeTax),
            IncomeTaxExpense = ParseDecimal(raw.IncomeTaxExpense),
            InterestAndDebtExpense = ParseDecimal(raw.InterestAndDebtExpense),
            NetIncomeFromContinuingOperations = ParseDecimal(raw.NetIncomeFromContinuingOperations),
            ComprehensiveIncomeNetOfTax = ParseDecimal(raw.ComprehensiveIncomeNetOfTax),
            EBIT = ParseDecimal(raw.EBIT),
            EBITDA = ParseDecimal(raw.EBITDA),
            NetIncome = ParseDecimal(raw.NetIncome),
        };
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0;
    }
}