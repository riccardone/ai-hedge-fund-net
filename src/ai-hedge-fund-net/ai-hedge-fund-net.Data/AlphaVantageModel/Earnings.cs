namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class Earnings
{
    public string Symbol { get; set; }
    public List<AnnualEarning> AnnualEarnings { get; set; }
    public List<QuarterlyEarning> QuarterlyEarnings { get; set; }
}

public class AnnualEarning
{
    public DateTime FiscalDateEnding { get; set; }
    public decimal ReportedEPS { get; set; }
}

public class QuarterlyEarning
{
    public DateTime FiscalDateEnding { get; set; }
    public DateTime ReportedDate { get; set; }
    public decimal ReportedEPS { get; set; }
    public decimal EstimatedEPS { get; set; }
    public decimal Surprise { get; set; }
    public decimal SurprisePercentage { get; set; }
}