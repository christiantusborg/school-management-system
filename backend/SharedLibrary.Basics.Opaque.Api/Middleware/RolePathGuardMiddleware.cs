namespace SharedLibrary.Basics.Opaque.Api.Middleware;

/// <summary>
/// Enforces role-based access by URL prefix. Each rule lists the roles allowed
/// to READ (safe methods) and the roles allowed to WRITE (POST/PUT/PATCH/DELETE):
/// <list type="bullet">
///   <item><c>/v1/admin/*</c> — <c>Admin</c> (read + write)</item>
///   <item><c>/v1/partner/*</c> — <c>Partner</c> (read + write)</item>
///   <item><c>/v1/student/me/*</c> — <c>Student</c> (read + write)</item>
///   <item><c>/v1/school/*</c> — System Config + shared catalogue: <c>Admin</c>
///     or <c>Partner</c> may read; only <c>Admin</c> may mutate. Nothing here is
///     public, so anonymous callers are rejected outright.</item>
/// </list>
/// All other paths pass through (login, register, /v1/public/*, OPAQUE flow,
/// MFA, etc.).
///
/// Why this exists: <c>app.MapGroup("/").AllowAnonymous()</c> in Program.cs
/// silently overrides the per-endpoint <c>RequireAuthorization("AdminOnly")</c>
/// decorators, so without this guard a Partner-authenticated user could call
/// the admin <c>approve-grades</c> endpoint and have their action recorded
/// as if Admission Office had approved — and an anonymous caller could create
/// or delete System Config rows (currencies, schools, lookup lists) outright.
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

    private sealed record Rule(string Prefix, string[] ReadRoles, string[] WriteRoles);

    // First matching prefix wins, so list more specific prefixes before broader
    // ones. For /v1/admin, /v1/partner and /v1/student the read and write role
    // sets are identical (the whole area is single-role), preserving the
    // original method-agnostic behaviour.
    private static readonly Rule[] _rules =
    [
        new("/v1/admin/",      ["Admin"],            ["Admin"]),
        new("/v1/partner/",    ["Partner"],          ["Partner"]),
        new("/v1/student/me/", ["Student"],          ["Student"]),
        new("/v1/school/",     ["Admin", "Partner"], ["Admin"]),
        // Intake authoring plane (questionnaire builder, templates, rules …).
        // Fill-out surfaces for students/partners/public land on their own
        // role-appropriate prefixes in later phases, never under /v1/intake.
        new("/v1/intake/",     ["Admin"],            ["Admin"]),
    ];

    private static readonly HashSet<string> _writeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var rule in _rules)
        {
            if (!path.StartsWith(rule.Prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var roles = _writeMethods.Contains(context.Request.Method)
                ? rule.WriteRoles
                : rule.ReadRoles;

            var user = context.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required.");
                return;
            }
            if (!roles.Any(user.IsInRole))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(roles.Length == 1
                    ? $"This endpoint requires the '{roles[0]}' role."
                    : $"This endpoint requires one of these roles: {string.Join(", ", roles)}.");
                return;
            }

            // Teacher partner users are read-only across the whole portal,
            // with exactly two allowed writes: saving grade DRAFTS (never
            // submitting) and commenting on uploaded assignments. Checked
            // only on partner writes so reads stay lookup-free.
            if (_writeMethods.Contains(context.Request.Method)
                && user.IsInRole("Partner")
                && !IsTeacherAllowedWrite(path)
                && await IsTeacherAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(
                    "Teacher accounts are read-only (except grade drafts and assignment comments).");
                return;
            }
            break;
        }

        await _next(context);
    }

    /// <summary>Code-level switch: teachers may upload assignments like any
    /// other partner user. Flip to false to make teachers comment-only on
    /// assignments again — no redeploy of anything else needed.</summary>
    private const bool TeacherCanUploadAssignments = true;

    private static bool IsTeacherAllowedWrite(string path) =>
        path.EndsWith("/grades/draft", StringComparison.OrdinalIgnoreCase)
        || (path.Contains("/assignments/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("/comments", StringComparison.OrdinalIgnoreCase))
        || (TeacherCanUploadAssignments
            && path.EndsWith("/assignments", StringComparison.OrdinalIgnoreCase))
        // Teacher portal: uploads on the teacher's OWN cohorts (endpoints
        // verify ownership), and editing the partner-editable fields of the
        // teacher's OWN faculty profile.
        || (path.Contains("/partner/my/cohorts/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("/files", StringComparison.OrdinalIgnoreCase))
        || path.Contains("/partner/my/cohort-files/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/partner/my/teachers/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/partner/my/faculty-files", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> IsTeacherAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return false;
        var db = context.RequestServices.GetRequiredService<Odin.Api.Base.Data.OdinDbContext>();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            db.Users.Where(u => u.Id == userId && u.IsTeacher));
    }
}
