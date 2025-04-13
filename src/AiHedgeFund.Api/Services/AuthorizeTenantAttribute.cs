using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiHedgeFund.Api.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var routeTenantId = context.RouteData.Values["tenantId"]?.ToString();
        var validatedTenantId = context.HttpContext.Items["TenantId"] as string;

        if (string.IsNullOrWhiteSpace(routeTenantId) || string.IsNullOrWhiteSpace(validatedTenantId))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Tenant authentication missing." });
            return;
        }

        if (!string.Equals(routeTenantId, validatedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ForbidResult("Tenant ID in route does not match authenticated tenant.");
        }
    }
}