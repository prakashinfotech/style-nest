using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StyleNest.Admin.API.Filters;

/// <summary>
/// ENH-AUTH-012 — Global authorization filter for Admin.API.
/// Any authenticated admin/superadmin request without the `mfa_verified=true` claim
/// is rejected with HTTP 403 ADMIN_MFA_REQUIRED.
/// Unauthenticated requests bypass this filter (handled by [Authorize] on controllers).
/// </summary>
public sealed class RequireMfaFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return; // not authenticated — let [Authorize] handle it

        if (!user.HasClaim("mfa_verified", "true"))
        {
            context.Result = new ObjectResult(new
            {
                errorCode = "ADMIN_MFA_REQUIRED",
                message   = "Admin access requires MFA verification. Complete MFA at POST /api/v1/auth/mfa/verify.",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
