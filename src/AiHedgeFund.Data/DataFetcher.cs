using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Data;

public class DataFetcher 
{
    private readonly ILogger<DataFetcher> _logger;
    private readonly FileDataManager _dataManager;
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly HttpClient _client;

    public DataFetcher(IHttpClientFactory httpClientFactory, FileDataManager dataManager, ILogger<DataFetcher> logger)
    {
        _logger = logger;
        _dataManager = dataManager;
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    /// <summary>
    /// Loads mapped data from memory → file cache → HTTP, in that order.
    /// </summary>
    /// <param name="isRawValid">
    /// Optional guard called on data read from the file cache.
    /// If the guard returns false the cache entry is deleted and the data is re-fetched from the API.
    /// This prevents previously-cached provider error responses (e.g. empty time series, null-symbol
    /// objects) from being silently treated as valid data.
    /// </param>
    public bool TryLoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper, out T? result, Func<TRaw, bool>? isRawValid = null)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            result = typed;
            return true;
        }

        var raw = _dataManager.Read<TRaw>(key);

        if (raw != null && isRawValid != null && !isRawValid(raw))
        {
            _logger.LogWarning("Cached data for '{Key}' failed validation (likely a stale error response) — deleting and re-fetching", key);
            _dataManager.Delete(key);
            raw = default;
        }

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
                _logger.LogWarning("API call to '{0}' failed with status code {1}", endpoint, response.StatusCode);
                result = default;
                return false;
            }

            var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (TryExtractProviderError(jsonString, out var providerError))
            {
                _logger.LogError("Alpha Vantage returned an error for '{0}': {1}", endpoint, providerError);
                result = default;
                return false;
            }

            result = JsonSerializer.Deserialize<T>(jsonString, options);
            if (result != null) return true;
            _logger.LogWarning("Deserialization returned null for endpoint '{0}'", endpoint);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP request failed {ex.GetBaseException().Message} '{endpoint}'");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON deserialization failed {ex.GetBaseException().Message} '{endpoint}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ex.GetBaseException().Message} '{endpoint}'");
        }

        result = default;
        return false;
    }

    private static bool TryExtractProviderError(string jsonString, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            foreach (var field in new[] { "Error Message", "Information", "Note" })
            {
                if (root.TryGetProperty(field, out var prop))
                {
                    errorMessage = prop.GetString() ?? field;
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // not valid JSON — let the main deserializer handle it
        }

        return false;
    }
}