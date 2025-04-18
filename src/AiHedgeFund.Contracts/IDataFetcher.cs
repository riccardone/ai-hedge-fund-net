namespace AiHedgeFund.Contracts;

public interface IDataFetcher
{
    bool TryLoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper, out T? result);
}