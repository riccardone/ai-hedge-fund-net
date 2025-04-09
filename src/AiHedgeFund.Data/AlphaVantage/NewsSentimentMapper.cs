using System.Globalization;
using System.Text.Json;
using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Data.AlphaVantage;

public static class NewsSentimentMapper
{
    public static List<NewsSentiment> Map(NewsSentimentRaw raw)
    {
        var result = new List<NewsSentiment>();

        if (raw?.Feed == null)
            return result;

        foreach (var item in raw.Feed)
        {
            var sentiment = new NewsSentiment
            {
                Title = item.TryGetValue("title", out var title) ? title?.ToString() : null,
                Url = item.TryGetValue("url", out var url) ? url?.ToString() : null,
                PublishedAt = ParseDate(item.TryGetValue("time_published", out var timePublished) ? timePublished?.ToString() : null),
                OverallSentimentScore = TryParseDecimal(item, "overall_sentiment_score"),
                OverallSentimentLabel = item.TryGetValue("overall_sentiment_label", out var label) ? label?.ToString() : null
            };

            // Parse ticker_sentiment
            if (item.TryGetValue("ticker_sentiment", out var tickerSentimentObj) &&
                tickerSentimentObj is JsonElement tickerSentimentArray &&
                tickerSentimentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var ts in tickerSentimentArray.EnumerateArray())
                {
                    sentiment.TickerSentiments.Add(new TickerSentiment
                    {
                        Ticker = ts.TryGetProperty("ticker", out var t) ? t.GetString() : null,
                        RelevanceScore = ts.TryGetProperty("relevance_score", out var rs) && decimal.TryParse(rs.GetString(), out var relevance) ? relevance : (decimal?)null,
                        SentimentScore = ts.TryGetProperty("ticker_sentiment_score", out var ss) && decimal.TryParse(ss.GetString(), out var score) ? score : (decimal?)null,
                        SentimentLabel = ts.TryGetProperty("ticker_sentiment_label", out var sl) ? sl.GetString() : null
                    });
                }
            }

            result.Add(sentiment);
        }
        
        return result;
    }

    private static DateTime? ParseDate(string? raw)
    {
        // Format: 20240408T143000
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateTime.TryParseExact(raw, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;
    }

    private static decimal? TryParseDecimal(Dictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var val) && decimal.TryParse(val?.ToString(), out var dec) ? dec : (decimal?)null;
    }
}