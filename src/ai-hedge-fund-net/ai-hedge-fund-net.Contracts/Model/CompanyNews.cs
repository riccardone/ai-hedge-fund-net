namespace ai_hedge_fund_net.Contracts.Model;

public class CompanyNews
{
    public string Ticker { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Source { get; set; }
    public DateTime Date { get; set; }
    public string Url { get; set; }
    public string? Sentiment { get; set; }
}