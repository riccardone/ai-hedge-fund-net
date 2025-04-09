namespace AiHedgeFund.Contracts.Model;

public class NewsSentiment
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public DateTime? PublishedAt { get; set; }
    public decimal? OverallSentimentScore { get; set; }
    public string? OverallSentimentLabel { get; set; }

    public List<TickerSentiment> TickerSentiments { get; set; } = new();
}

public class TickerSentiment
{
    public string? Ticker { get; set; }
    public decimal? RelevanceScore { get; set; }
    public decimal? SentimentScore { get; set; }
    public string? SentimentLabel { get; set; }
}