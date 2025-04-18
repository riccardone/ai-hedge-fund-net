namespace AiHedgeFund.Contracts;

public interface IAuthChecker
{
    bool Check(string tenantId, string apiKey, out string[] errors, out string[] roles);
}