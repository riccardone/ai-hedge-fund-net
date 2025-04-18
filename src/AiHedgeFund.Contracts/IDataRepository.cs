namespace AiHedgeFund.Contracts;

public interface IDataRepository
{
    bool TryRead<T>(string key, out T? data);
    bool TrySave<T>(T data, string key);
}