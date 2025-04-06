using System.Text.Json.Serialization;

namespace AiHedgeFund.Data.AlphaVantage;

public class IncomeStatementRaw
{
    public string Symbol { get; set; }

    [JsonPropertyName("annualReports")]
    public List<Dictionary<string, string>> AnnualReports { get; set; }

    [JsonPropertyName("quarterlyReports")]
    public List<Dictionary<string, string>> QuarterlyReports { get; set; }
}