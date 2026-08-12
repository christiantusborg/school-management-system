using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// "Handled by" — assigns a student to a Sales staff member. Payments and
/// status switches then credit that person in the Signups statistics
/// (signups themselves stay with Added by), and the student appears in that
/// Sales login's scoped portal view. Set by Editor level and above; Sales
/// and Viewer are read-only. GET /v1/admin/sales-staff feeds the dropdown.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/handled-by")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsHandledByEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/sales-staff", ListSalesStaffAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/students/{studentId:guid}/handled-by", SetAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> ListSalesStaffAsync(OdinDbContext db, CancellationToken ct)
    {
        var sales = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            join u in db.Users on ur.UserId equals u.Id
            where r.Name == "Sales" && u.IsEnabled && u.DeletedAt == null
            select new
            {
                userId = u.Id,
                name = db.UserProfiles.Where(p => p.UserId == u.Id)
                    .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim())
                    .FirstOrDefault(),
                userName = u.UserName,
            }).ToListAsync(ct);
        return Results.Ok(new
        {
            items = sales
                .Select(s => new { s.userId, name = string.IsNullOrWhiteSpace(s.name) ? s.userName : s.name })
                .OrderBy(s => s.name)
                .ToList(),
        });
    }

    public sealed class SetBody { public string? UserId { get; init; } }

    private static async Task<IResult> SetAsync(
        Guid studentId, [FromBody] SetBody body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(httpContext.User, AdminPermissions.StudentsSetHandledBy, ct))
            return Results.Json(new { error = "Requires Editor level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(body.UserId))
        {
            student.HandledByUserId = null;
        }
        else
        {
            var isSales = await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                where r.Name == "Sales" && ur.UserId == body.UserId
                select ur.UserId).AnyAsync(ct);
            if (!isSales) return Results.BadRequest(new { error = "Handled by must be a Sales staff user." });
            student.HandledByUserId = body.UserId;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentId, handledByUserId = student.HandledByUserId });
    }
}
