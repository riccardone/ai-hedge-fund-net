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
    public decimal? EarningsPerShare { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? OperatingIncome { get; set; }

    // Other fields not directly mapped above
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