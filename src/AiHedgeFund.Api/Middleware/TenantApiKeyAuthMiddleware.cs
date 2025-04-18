using System.Security.Claims;
using NLog;
using AiHedgeFund.Contracts;

namespace AiHedgeFund.Api.Middleware;

public class TenantApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public TenantApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthChecker authChecker)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var providedKey) ||
            !context.Request.Headers.TryGetValue("X-TENANT-ID", out var providedTenant))
        {
            Logger.Warn("Missing authentication headers.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key or Tenant ID is missing.");
            return;
        }

        var tenantId = providedTenant.ToString();
        var apiKey = providedKey.ToString();

        if (!authChecker.Check(tenantId, apiKey, out var errors, out var roles))
        {
            Logger.Warn("Authentication failed for tenant {0}: {1}", tenantId, string.Join("; ", errors));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Authorization failed: " + string.Join("; ", errors));
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tenantId),
            new("TenantId", tenantId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "HeaderAuth"));
        context.Items["TenantId"] = tenantId;

        Logger.Debug("User roles: {0}", string.Join(", ", context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)));

        Logger.Debug("Authenticated tenant {0} with roles: {1}", tenantId, string.Join(", ", roles));
        await _next(context);
    }
}