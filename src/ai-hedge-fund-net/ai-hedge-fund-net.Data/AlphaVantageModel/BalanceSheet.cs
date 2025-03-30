namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class BalanceSheet
{
    public string Symbol { get; set; }
    public List<BalanceSheetReport> AnnualReports { get; set; }
    public List<BalanceSheetReport> QuarterlyReports { get; set; }
}

public class BalanceSheetReport
{
    public DateTime FiscalDateEnding { get; set; }
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalCurrentAssets { get; set; }
    public decimal? TotalCurrentLiabilities { get; set; }
    public long? TotalShareholderEquity { get; set; }
    public long? CommonStockSharesOutstanding { get; set; }

    public Dictionary<string, decimal?> GetLineItems()
    {
        return new Dictionary<string, decimal?>
        {
            ["TotalAssets"] = TotalAssets,
            ["TotalLiabilities"] = TotalLiabilities,
            ["TotalCurrentAssets"] = TotalAssets,
            ["TotalCurrentLiabilities"] = TotalLiabilities,
            ["TotalShareholderEquity"] = TotalShareholderEquity
        };
    }
}