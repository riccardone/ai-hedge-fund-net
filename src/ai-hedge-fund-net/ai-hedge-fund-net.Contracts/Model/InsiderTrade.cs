namespace ai_hedge_fund_net.Contracts.Model;

public class InsiderTrade
{
    public string Ticker { get; set; }
    public string? Issuer { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public bool? IsBoardDirector { get; set; }
    public string? TransactionDate { get; set; }
    public decimal? TransactionShares { get; set; }
    public decimal? TransactionPricePerShare { get; set; }
    public decimal? TransactionValue { get; set; }
    public decimal? SharesOwnedBeforeTransaction { get; set; }
    public decimal? SharesOwnedAfterTransaction { get; set; }
    public string? SecurityTitle { get; set; }
    public DateTime FilingDate { get; set; }
}