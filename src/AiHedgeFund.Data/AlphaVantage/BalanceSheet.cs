namespace AiHedgeFund.Data.AlphaVantage;

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
    public decimal? CommonStockSharesOutstanding { get; set; }
    public decimal? CashAndCashEquivalentsAtCarryingValue { get; set; }
    public decimal? ShortTermDebt { get; set; }
    public decimal? LongTermDebt { get; set; }
    public decimal? Goodwill { get; set; }
    public decimal? IntangibleAssets { get; set; }

    public decimal? GoodwillAndIntangibleAssets => Goodwill.HasValue && IntangibleAssets.HasValue
        ? Goodwill + IntangibleAssets
        : IntangibleAssets ?? (Goodwill ?? 0);

    public Dictionary<string, decimal?> GetLineItems()
    {
        return new Dictionary<string, decimal?>
        {
            ["TotalAssets"] = TotalAssets,
            ["TotalLiabilities"] = TotalLiabilities,
            ["TotalCurrentAssets"] = TotalAssets,
            ["TotalCurrentLiabilities"] = TotalLiabilities,
            ["TotalShareholderEquity"] = TotalShareholderEquity,
            ["ShortTermDebt"] = ShortTermDebt,
            ["LongTermDebt"] = LongTermDebt,
            ["Goodwill"] = Goodwill,
            ["IntangibleAssets"] = IntangibleAssets,
        };
    }
}