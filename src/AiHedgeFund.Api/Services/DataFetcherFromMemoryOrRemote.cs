using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using AiHedgeFund.Contracts;
using NLog;

namespace AiHedgeFund.Api.Services;

public class DataFetcherFromMemoryOrRemote : IDataFetcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly HttpClient _client;

    public DataFetcherFromMemoryOrRemote(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    public bool TryLoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper, out T? result)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            result = typed;
            return true;
        }

        if (!TryFetchData(query, out TRaw results))
        {
            result = default;
            return false;
        }

        if (results == null)
        {
            result = default;
            return false;
        }
        var mapped = mapper(results);
        _memoryCache[key] = mapped!;
        result = mapped;
        return true;
    }

    private bool TryFetchData<T>(string endpoint, out T result) 
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
            if(result != null && TryValidateResult(result))
                return true;
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

    private bool TryValidateResult<T>(T result)
    {
        if (result == null)
            return false;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Only check readable properties
            if (!property.CanRead)
                continue;

            var value = property.GetValue(result);
            if (value == null)
                return false;
            // not all null
            return true;
        }

        return true;
    }
}