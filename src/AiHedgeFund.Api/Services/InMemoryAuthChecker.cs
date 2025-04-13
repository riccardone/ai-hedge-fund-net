using AiHedgeFund.Contracts;

namespace AiHedgeFund.Api.Services;

public class InMemoryAuthChecker : IAuthChecker
{
    private readonly Dictionary<string, string> _tenantApiKeys = new()
    {
        { "tenant-123", "secret-key-123" },
        { "tenant-abc", "secret-key-abc" }
    };

    public bool CheckApiKey(string tenantId, string apiKey)
    {
        return _tenantApiKeys.TryGetValue(tenantId, out var storedKey) && storedKey == apiKey;
    }

    public bool Check(string tenantId, string apiKey, out List<string> errors, AuthorizationDelegate? func = null)
    {
        errors = new List<string>();

        if (!_tenantApiKeys.TryGetValue(tenantId, out var storedKey))
        {
            errors.Add("Tenant ID not found.");
            return false;
        }

        if (storedKey != apiKey)
        {
            errors.Add("Invalid API key.");
            return false;
        }

        if (func != null && !func(apiKey, out var funcError))
        {
            errors.Add(funcError);
            return false;
        }

        return true;
    }
}