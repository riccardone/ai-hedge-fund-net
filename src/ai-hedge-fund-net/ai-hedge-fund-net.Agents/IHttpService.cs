namespace ai_hedge_fund_net.Agents;

public interface IHttpService
{
    bool TryPost(string path, string payload, out string response);
}
