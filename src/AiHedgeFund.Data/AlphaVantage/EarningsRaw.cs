namespace AiHedgeFund.Data.AlphaVantage;

public class EarningsRaw
{
    public string Symbol { get; set; }
    public List<AnnualEarningRaw> AnnualEarnings { get; set; }
    public List<QuarterlyEarningRaw> QuarterlyEarnings { get; set; }
}

public class AnnualEarningRaw
{
    public string FiscalDateEnding { get; set; }
    public string ReportedEPS { get; set; }
}

public class QuarterlyEarningRaw
{
    public string FiscalDateEnding { get; set; }
    public string ReportedDate { get; set; }
    public string ReportedEPS { get; set; }
    public string EstimatedEPS { get; set; }
    public string Surprise { get; set; }
    public string SurprisePercentage { get; set; }
}
