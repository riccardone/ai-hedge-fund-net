namespace AiHedgeFund.Contracts;

public interface IChatter
{
    bool TryPost(string path, string payload, out string response);
}