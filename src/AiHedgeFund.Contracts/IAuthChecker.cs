namespace AiHedgeFund.Contracts;

public interface IAuthChecker
{
    bool CheckApiKey(string tenantId, string apiKey);
    bool Check(string tenantId, string apiKey, out List<string> errors, AuthorizationDelegate? func = null);
}

public delegate bool AuthorizationDelegate(string apiKey, out string error);