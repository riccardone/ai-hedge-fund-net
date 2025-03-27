namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class IncomeStatementRaw
{
    public string Symbol { get; set; }
    public List<Dictionary<string, string>> AnnualReports { get; set; }
    public List<Dictionary<string, string>> QuarterlyReports { get; set; }
}