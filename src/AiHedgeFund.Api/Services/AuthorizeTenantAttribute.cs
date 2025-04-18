using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiHedgeFund.Api.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var validatedTenantId = context.HttpContext.Items["TenantId"] as string;

        if (string.IsNullOrWhiteSpace(validatedTenantId))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Tenant authentication failed or missing." });
        }
    }
}