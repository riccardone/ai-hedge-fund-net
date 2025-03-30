using System.Text.Json;
using ai_hedge_fund_net.Contracts;

namespace ai_hedge_fund_net.Data.Finnhub;

public class FinnhubVolumeProvider : IPriceVolumeProvider
{
    private readonly HttpClient _httpClient;

    public FinnhubVolumeProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Finnhub");
    }

    public decimal? GetVolume(string ticker, DateTime date)
    {
        var from = ((DateTimeOffset)date.Date).ToUnixTimeSeconds();
        var to = ((DateTimeOffset)date.Date.AddDays(1)).ToUnixTimeSeconds();

        var query = $"stock/candle?symbol={ticker}&resolution=D&from={from}&to={to}";
        var response = _httpClient.GetAsync(query).Result;
        if (!response.IsSuccessStatusCode)
            return null;

        var json = response.Content.ReadAsStringAsync().Result;
        var candle = JsonSerializer.Deserialize<FinnhubCandleResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return candle?.V?.FirstOrDefault();
    }
}