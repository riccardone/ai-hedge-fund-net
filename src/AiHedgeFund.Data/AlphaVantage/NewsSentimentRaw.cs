using System.Text.Json.Serialization;

namespace AiHedgeFund.Data.AlphaVantage;

public class NewsSentimentRaw
{
    [JsonPropertyName("feed")]
    public List<Dictionary<string, object>>? Feed { get; set; }
}