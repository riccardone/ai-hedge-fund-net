using AiHedgeFund.Contracts;

namespace AiHedgeFund.Tests.Fakes;

public class FakeHttpLib : IHttpLib
{
    public bool TryPost(string path, string payload, out string response)
    {
        throw new NotImplementedException();
    }
}