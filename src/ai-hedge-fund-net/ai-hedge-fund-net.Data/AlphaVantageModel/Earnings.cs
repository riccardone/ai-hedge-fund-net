namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class Earnings
{
    public string Symbol { get; set; }
    public List<EarningsReport> AnnualEarnings { get; set; }
    public List<EarningsReport> QuarterlyEarnings { get; set; }
}

public class EarningsReport
{
    public DateTime FiscalDateEnding { get; set; }
    public DateTime? ReportedDate { get; set; } // Only for quarterly earnings
    public decimal? ReportedEPS { get; set; }
    public decimal? EstimatedEPS { get; set; } // Only for quarterly earnings
    public decimal? Surprise { get; set; } // Only for quarterly earnings
    public decimal? SurprisePercentage { get; set; } // Only for quarterly earnings
}