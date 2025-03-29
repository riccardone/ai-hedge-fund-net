using System.Collections.Concurrent;
using System.Text.Json;
using ai_hedge_fund_net.Contracts;
using NLog;

namespace ai_hedge_fund_net.Data;

public class DataFetcher : IDataFetcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IDataManager _dataManager;
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly HttpClient _client;

    public DataFetcher(IDataManager dataManager, IHttpClientFactory httpClientFactory)
    {
        _dataManager = dataManager;
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    public T? LoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
            return typed;

        var raw = _dataManager.Read<TRaw>(key);

        if (raw == null && TryFetchData(query, out raw))
        {
            _dataManager.Save(raw, key);
        }

        if (raw == null) return default;
        var mapped = mapper(raw);
        _memoryCache[key] = mapped!;
        return mapped;
    }

    private bool TryFetchData<T>(string endpoint, out T? result) 
    {
        try
        {
            var response = _client.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn("API call to '{0}' failed with status code {1}", endpoint, response.StatusCode);
                result = default;
                return false;
            }

            var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            result = JsonSerializer.Deserialize<T>(jsonString, options);
            if (result != null) return true;
            Logger.Warn("Deserialization returned null for endpoint '{0}'", endpoint);
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "HTTP request failed for endpoint '{0}'", endpoint);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON deserialization failed for endpoint '{0}'", endpoint);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error in TryFetchData for endpoint '{0}'", endpoint);
        }

        result = default;
        return false;
    }
}