using AiHedgeFund.Api.Middleware;

namespace AiHedgeFund.Api.Services;

public static class TenantApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantApiKeyAuthMiddleware>();
    }
}