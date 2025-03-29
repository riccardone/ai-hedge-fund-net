namespace ai_hedge_fund_net.Contracts;

public interface IDataFetcher
{
    T? LoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper);
}