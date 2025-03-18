namespace ai_hedge_fund_net.Contracts;

public interface IChatter
{
    bool TryPost(string path, string payload, out string response);
}