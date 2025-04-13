using AiHedgeFund.Contracts;
using NLog;

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

        if (!authChecker.Check(tenantId, apiKey, out var errors))
        {
            Logger.Warn("Authentication failed for tenant {0}: {1}", tenantId, string.Join("; ", errors));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Authorization failed: " + string.Join("; ", errors));
            return;
        }

        Logger.Debug("Authenticated tenant {0} successfully", tenantId);
        context.Items["TenantId"] = tenantId;

        await _next(context);
    }
}