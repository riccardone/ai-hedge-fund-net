namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class EarningsRaw
{
    public string Symbol { get; set; }
    public List<Dictionary<string, string>> AnnualEarnings { get; set; }
    public List<Dictionary<string, string>> QuarterlyEarnings { get; set; }
}