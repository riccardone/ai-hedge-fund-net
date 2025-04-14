using AiHedgeFund.Contracts;

namespace AiHedgeFund.Api.Services;

public class FakeDataRepository : IDataRepository
{
    public bool TrySave<T>(T data, string key)
    {
        return true;
    }

    public bool TryRead<T>(string key, out T? data)
    {
        data = default;
        return true;
    }
}