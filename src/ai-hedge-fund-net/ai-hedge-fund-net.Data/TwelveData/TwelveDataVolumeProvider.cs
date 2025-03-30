using System.Text.Json;
using ai_hedge_fund_net.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ai_hedge_fund_net.Data.TwelveData;

public class TwelveDataVolumeProvider : IPriceVolumeProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwelveDataVolumeProvider> _logger;
    private string _apiKey;

    public TwelveDataVolumeProvider(IHttpClientFactory httpClientFactory, ILogger<TwelveDataVolumeProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("TwelveData");
        _logger = logger;
        _apiKey = configuration["TwelveData:ApiKey"] ?? throw new ArgumentNullException("TwelveData:ApiKey is missing");
    }

    public decimal? GetVolume(string ticker, DateTime date)
    {
        const int maxLookbackDays = 5;
        for (int i = 0; i < maxLookbackDays; i++)
        {
            var attemptDate = date.AddDays(-i);
            var dateStr = attemptDate.ToString("yyyy-MM-dd");

            var url = $"time_series?symbol={ticker}&interval=1day&start_date={dateStr}&end_date={dateStr}&outputsize=1&apikey={_apiKey}";

            try
            {
                var response = _httpClient.GetAsync(url).Result;
                var json = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("TwelveData call failed for {Ticker} on {Date}: {Json}", ticker, attemptDate, json);
                    continue;
                }

                var result = JsonSerializer.Deserialize<TwelveDataResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var volumeStr = result?.Values?.FirstOrDefault()?.Volume;
                if (decimal.TryParse(volumeStr, out var volume))
                    return volume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking volume for {Ticker} on {Date}", ticker, date);
            }
        }

        _logger.LogWarning("No volume found for {Ticker} within last {Days} days from {StartDate}", ticker, maxLookbackDays, date);
        return null;
    }
}