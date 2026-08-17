using Hangfire.Dashboard;

namespace StyleNest.Auth.API.Filters;

/// <summary>
/// ENH-ADMIN-002 — Hangfire Dashboard admin-only authorization filter.
///
/// Grants access when the requesting user is authenticated with the Admin or SuperAdmin role
/// (JWT Bearer token validated by the UseAuthentication middleware that runs before the
/// UseHangfireDashboard registration in the pipeline).
///
/// Dev convenience: loopback addresses (127.0.0.1 / ::1) are always permitted so that
/// local developers can browse /hangfire without embedding a Bearer token in the browser.
/// In production this branch is never reached from outside the host.
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Primary check: authenticated JWT + Admin or SuperAdmin role
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.IsInRole("Admin")
                || httpContext.User.IsInRole("SuperAdmin");
        }

        // Dev convenience: allow loopback access without a token so developers
        // can open http://localhost:5001/hangfire directly during local debugging.
        // This branch is unreachable from outside the host machine.
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        return remoteIp is not null && System.Net.IPAddress.IsLoopback(remoteIp);
    }
}
