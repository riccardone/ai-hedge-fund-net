using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Data.ChartLibrary;

/// <summary>
/// Degrade-safe client over Chart Library's cohort_analyze endpoint. A failed or
/// disabled lookup returns false with a null base rate and never throws — grounding
/// is advisory and must never block the trading flow. Results (including negative
/// ones) are cached per run so a slow endpoint is hit at most once per (symbol, date).
/// </summary>
public sealed class ChartLibraryBaseRateProvider : IBaseRateProvider
{
    private readonly HttpClient _client;
    private readonly GroundingOptions _options;
    private readonly ILogger<ChartLibraryBaseRateProvider> _logger;
    private readonly ConcurrentDictionary<string, BaseRate?> _cache = new();

    public ChartLibraryBaseRateProvider(
        IHttpClientFactory clientFactory,
        GroundingOptions options,
        ILogger<ChartLibraryBaseRateProvider> logger)
    {
        _client = clientFactory.CreateClient("ChartLibrary");
        _options = options;
        _logger = logger;
    }

    public bool Enabled => _options.Active;

    public bool TryGetBaseRate(string symbol, DateTime asOf, out BaseRate? baseRate)
    {
        baseRate = null;
        if (!Enabled || string.IsNullOrWhiteSpace(symbol))
            return false;

        var sym = symbol.Trim().ToUpperInvariant();
        var day = asOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var horizon = _options.OverfitHorizon;
        var cacheKey = $"{sym}|{day}|{horizon}";

        var result = _cache.GetOrAdd(cacheKey, _ => Fetch(sym, day, horizon));
        baseRate = result;
        return result is not null;
    }

    private BaseRate? Fetch(string symbol, string day, int horizon)
    {
        try
        {
            var body = BuildRequestBody(symbol, day);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/cohort_analyze")
            {
                Content = content
            };
            if (_options.HasKey)
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");

            var timeout = _options.TimeoutSeconds <= 0 ? 8.0 : _options.TimeoutSeconds;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            // Synchronous over the async API to match the codebase's IHttpLib pattern.
            using var response = _client.SendAsync(request, cts.Token).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Base-rate grounding: HTTP {Status} for {Symbol} {Day}",
                    (int)response.StatusCode, symbol, day);
                return null;
            }

            var json = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();
            return BaseRateParser.Parse(json, symbol, day, horizon);
        }
        catch (Exception ex)
        {
            // Degrade-safe by contract: any failure leaves the agent exactly as it is today.
            _logger.LogDebug(ex, "Base-rate grounding unavailable for {Symbol} {Day}", symbol, day);
            return null;
        }
    }

    private string BuildRequestBody(string symbol, string day)
    {
        var body = new
        {
            anchor = new { symbol, date = day, timeframe = _options.Timeframe },
            horizons = _options.Horizons,
            options = new { exclude_same_symbol_days = _options.ExcludeSameSymbolDays }
        };
        return JsonSerializer.Serialize(body);
    }
}
