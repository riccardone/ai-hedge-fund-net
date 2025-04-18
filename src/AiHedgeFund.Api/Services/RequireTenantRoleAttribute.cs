using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiHedgeFund.Api.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _requiredRoles;

    public RequireTenantRoleAttribute(params string[] roles)
    {
        _requiredRoles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasRole = _requiredRoles.Any(role => user.IsInRole(role));
        if (!hasRole)
        {
            context.Result = new ForbidResult();
        }
    }
}