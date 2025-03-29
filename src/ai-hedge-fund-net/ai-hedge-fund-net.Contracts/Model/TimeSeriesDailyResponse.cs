using System.Text.Json.Serialization;

namespace ai_hedge_fund_net.Contracts.Model;

public class TimeSeriesDailyResponse
{
    [JsonPropertyName("Time Series (Daily)")]
    public Dictionary<string, Dictionary<string, string>> TimeSeries { get; set; } = new();
}