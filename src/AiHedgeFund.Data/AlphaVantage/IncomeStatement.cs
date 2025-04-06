namespace AiHedgeFund.Data.AlphaVantage;

public class IncomeStatement
{
    public string Symbol { get; set; }
    public List<IncomeStatementReport> AnnualReports { get; set; } = new();
    public List<IncomeStatementReport> QuarterlyReports { get; set; } = new();
}

public class IncomeStatementReport
{
    public DateTime FiscalDateEnding { get; set; }
    public string ReportedCurrency { get; set; }

    // Nullable in case Alpha Vantage omits them
    public decimal? NetIncome { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? OperatingIncome { get; set; }

    // Holds all other fields not directly mapped above
    public Dictionary<string, decimal?> Extras { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal?> GetLineItems()
    {
        return new Dictionary<string, decimal?>
        {
            ["NetIncome"] = NetIncome,
            ["EarningsPerShare"] = EarningsPerShare,
            ["TotalRevenue"] = Extras.TryGetValue("TotalRevenue", out var totalRevenue) ? totalRevenue : null,
            ["GrossProfit"] = Extras.TryGetValue("GrossProfit", out var grossProfit) ? grossProfit : null,
        };
    }
}


//namespace AiHedgeFund.Data.AlphaVantage;

//public class IncomeStatement
//{
//    public string Symbol { get; set; }
//    public List<IncomeStatementReport> AnnualReports { get; set; }
//    public List<IncomeStatementReport> QuarterlyReports { get; set; }
//}

//public class IncomeStatementReport
//{
//    public DateTime FiscalDateEnding { get; set; }
//    public string ReportedCurrency { get; set; }
//    public decimal GrossProfit { get; set; }
//    public decimal TotalRevenue { get; set; }
//    public decimal CostOfRevenue { get; set; }
//    public decimal CostOfGoodsAndServicesSold { get; set; }
//    public decimal OperatingIncome { get; set; }
//    public decimal SellingGeneralAndAdministrative { get; set; }
//    public decimal ResearchAndDevelopment { get; set; }
//    public decimal OperatingExpenses { get; set; }
//    public decimal InvestmentIncomeNet { get; set; }
//    public decimal NetInterestIncome { get; set; }
//    public decimal InterestIncome { get; set; }
//    public decimal InterestExpense { get; set; }
//    public decimal NonInterestIncome { get; set; }
//    public decimal OtherNonOperatingIncome { get; set; }
//    public decimal Depreciation { get; set; }
//    public decimal DepreciationAndAmortization { get; set; }
//    public decimal IncomeBeforeTax { get; set; }
//    public decimal IncomeTaxExpense { get; set; }
//    public decimal InterestAndDebtExpense { get; set; }
//    public decimal NetIncomeFromContinuingOperations { get; set; }
//    public decimal ComprehensiveIncomeNetOfTax { get; set; }
//    public decimal EBIT { get; set; }
//    public decimal EBITDA { get; set; }
//    public decimal? NetIncome { get; set; }

//    public decimal? EarningsPerShare { get; set; }

//    public Dictionary<string, decimal?> GetLineItems()
//    {
//        return new Dictionary<string, decimal?>
//        {
//            ["TotalRevenue"] = TotalRevenue,
//            ["GrossProfit"] = GrossProfit,
//            ["NetIncome"] = NetIncome,
//            ["EarningsPerShare"] = EarningsPerShare
//        };
//    }
//}