using AiHedgeFund.Contracts;

namespace AiHedgeFund.Api.Services;

public class InMemoryAuthChecker : IAuthChecker
{
    private readonly Dictionary<string, (string ApiKey, string[] Roles)> _tenantKeys = new()
    {
        { "tenant-a", ("api-key-123", new[] { "Trader", "Admin" }) },
        { "tenant-b", ("api-key-456", new[] { "Viewer" }) }
    };

    public bool Check(string tenantId, string apiKey, out string[] errors, out string[] roles)
    {
        errors = Array.Empty<string>();
        roles = Array.Empty<string>();

        if (!_tenantKeys.TryGetValue(tenantId, out var entry))
        {
            errors = new[] { "Unknown tenant" };
            return false;
        }

        if (!string.Equals(entry.ApiKey, apiKey, StringComparison.Ordinal))
        {
            errors = new[] { "Invalid API key" };
            return false;
        }

        roles = entry.Roles;
        return true;
    }
}