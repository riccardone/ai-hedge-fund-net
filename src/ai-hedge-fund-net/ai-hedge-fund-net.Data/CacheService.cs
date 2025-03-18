using ai_hedge_fund_net.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace ai_hedge_fund_net.Data;

public class CacheService : ICaching
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public CacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(60) // Adjust as needed
        };
    }

    private List<Dictionary<string, object>> MergeData(
        List<Dictionary<string, object>>? existing,
        List<Dictionary<string, object>> newData,
        string keyField)
    {
        if (existing == null)
            return newData;

        var existingKeys = existing.Select(item => item[keyField]?.ToString()).ToHashSet();
        existing.AddRange(newData.Where(item => item.ContainsKey(keyField) && !existingKeys.Contains(item[keyField]?.ToString())));

        return existing;
    }

    public List<Dictionary<string, object>>? GetPrices(string ticker) =>
        _cache.TryGetValue(ticker, out List<Dictionary<string, object>> prices) ? prices : null;

    public void SetPrices(string ticker, List<Dictionary<string, object>> data)
    {
        var existing = GetPrices(ticker);
        _cache.Set(ticker, MergeData(existing, data, "time"), _cacheOptions);
    }

    public List<Dictionary<string, object>>? GetFinancialMetrics(string ticker) =>
        _cache.TryGetValue(ticker, out List<Dictionary<string, object>> metrics) ? metrics : null;

    public void SetFinancialMetrics(string ticker, List<Dictionary<string, object>> data)
    {
        var existing = GetFinancialMetrics(ticker);
        _cache.Set(ticker, MergeData(existing, data, "report_period"), _cacheOptions);
    }

    public List<Dictionary<string, object>>? GetLineItems(string ticker) =>
        _cache.TryGetValue(ticker, out List<Dictionary<string, object>> lineItems) ? lineItems : null;

    public void SetLineItems(string ticker, List<Dictionary<string, object>> data)
    {
        var existing = GetLineItems(ticker);
        _cache.Set(ticker, MergeData(existing, data, "report_period"), _cacheOptions);
    }

    public List<Dictionary<string, object>>? GetInsiderTrades(string ticker) =>
        _cache.TryGetValue(ticker, out List<Dictionary<string, object>> trades) ? trades : null;

    public void SetInsiderTrades(string ticker, List<Dictionary<string, object>> data)
    {
        var existing = GetInsiderTrades(ticker);
        _cache.Set(ticker, MergeData(existing, data, "filing_date"), _cacheOptions);
    }

    public List<Dictionary<string, object>>? GetCompanyNews(string ticker) =>
        _cache.TryGetValue(ticker, out List<Dictionary<string, object>> news) ? news : null;

    public void SetCompanyNews(string ticker, List<Dictionary<string, object>> data)
    {
        var existing = GetCompanyNews(ticker);
        _cache.Set(ticker, MergeData(existing, data, "date"), _cacheOptions);
    }
}