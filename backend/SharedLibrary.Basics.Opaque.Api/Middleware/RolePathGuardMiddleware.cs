namespace SharedLibrary.Basics.Opaque.Api.Middleware;

/// <summary>
/// Enforces role-based access by URL prefix:
/// <list type="bullet">
///   <item><c>/v1/admin/*</c> — caller must be in the <c>Admin</c> role</item>
///   <item><c>/v1/partner/*</c> — caller must be in the <c>Partner</c> role</item>
///   <item><c>/v1/student/me/*</c> — caller must be in the <c>Student</c> role</item>
/// </list>
/// All other paths pass through (login, register, /v1/public/*, OPAQUE flow,
/// MFA, etc.).
///
/// Why this exists: <c>app.MapGroup("/").AllowAnonymous()</c> in Program.cs
/// silently overrides the per-endpoint <c>RequireAuthorization("AdminOnly")</c>
/// decorators, so without this guard a Partner-authenticated user could call
/// the admin <c>approve-grades</c> endpoint and have their action recorded
/// as if Admission Office had approved.
///
/// Returns <c>401</c> when the request has no authenticated identity, and
/// <c>403</c> when the user is authenticated but missing the required role.
/// Handler-level ownership / data scoping (e.g. partner-owns-student) still
/// applies on top of this.
/// </summary>
public sealed class RolePathGuardMiddleware
{
    private readonly RequestDelegate _next;
    public RolePathGuardMiddleware(RequestDelegate next) => _next = next;

    private static readonly (string Prefix, string Role)[] _rules =
    [
        ("/v1/admin/",      "Admin"),
        ("/v1/partner/",    "Partner"),
        ("/v1/student/me/", "Student"),
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var (prefix, role) in _rules)
        {
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var user = context.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required.");
                return;
            }
            if (!user.IsInRole(role))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync($"This endpoint requires the '{role}' role.");
                return;
            }
            break;
        }

        await _next(context);
    }
}
