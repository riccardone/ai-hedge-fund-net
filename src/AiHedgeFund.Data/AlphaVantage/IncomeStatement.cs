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
    public decimal? NetIncome { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? GrossProfit { get; set; }
    public decimal? OperatingIncome { get; set; }
    public decimal? OperatingMargin => OperatingIncome.HasValue && TotalRevenue.HasValue ? OperatingIncome / TotalRevenue : 0;
    public decimal? ResearchAndDevelopment { get; set; }
    public decimal? SellingGeneralAndAdministrative { get; set; }
    public decimal? InterestExpense { get; set; }
    public decimal? IncomeBeforeTax { get; set; }
    public decimal? CostOfRevenue { get; set; }
    public decimal? IncomeTaxExpense { get; set; }
    public decimal GrossMargin { get; set; }
    public Dictionary<string, decimal?> Extras { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal? EBITDA { get; set; }
    public decimal? EBIT { get; set; }

    public Dictionary<string, decimal?> GetLineItems()
    {
        return new Dictionary<string, decimal?>
        {
            ["NetIncome"] = NetIncome,
            ["TotalRevenue"] = TotalRevenue,
            ["GrossProfit"] = GrossProfit,
            ["OperatingIncome"] = OperatingIncome,
            ["OperatingMargin"] = OperatingMargin,
            ["ResearchAndDevelopment"] = ResearchAndDevelopment,
            ["SellingGeneralAndAdministrative"] = SellingGeneralAndAdministrative,
            ["InterestExpense"] = InterestExpense,
            ["IncomeBeforeTax"] = IncomeBeforeTax,
            ["CostOfRevenue"] = CostOfRevenue,
            ["IncomeTaxExpense"] = IncomeTaxExpense,
            ["GrossMargin"] = GrossMargin,
            ["EBITDA"] = EBITDA,
            ["EBIT"] = EBIT
        };
    }
}