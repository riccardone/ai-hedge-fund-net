using System.Collections.Concurrent;
using System.Text.Json;
using NLog;

namespace AiHedgeFund.Data;

public class DataFetcher 
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly FileDataManager _dataManager;
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly HttpClient _client;

    public DataFetcher(IHttpClientFactory httpClientFactory)
    {
        _dataManager = new FileDataManager();
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    public bool TryLoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper, out T? result)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            result = typed;
            return true;
        }

        var raw = _dataManager.Read<TRaw>(key);

        if (raw == null && TryFetchData(query, out raw))
        {
            _dataManager.Save(raw, key);
        }

        if (raw == null)
        {
            result = default;
            return false;
        }
        var mapped = mapper(raw);
        _memoryCache[key] = mapped!;
        result = mapped;
        return true;
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

            if (jsonString.Contains("detected your API key"))
                throw new ArgumentException($"Error message from the financial data provider: {jsonString}");

            result = JsonSerializer.Deserialize<T>(jsonString, options);
            if (result != null) return true;
            Logger.Warn("Deserialization returned null endpoint '{0}'", endpoint);
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, $"HTTP request failed {ex.GetBaseException().Message} '{endpoint}'");
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, $"JSON deserialization failed {ex.GetBaseException().Message} '{endpoint}'");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"{ex.GetBaseException().Message} '{endpoint}'");
        }

        result = default;
        return false;
    }
}