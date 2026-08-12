using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Partners;

/// <summary>
/// Sales↔partner assignments, editable from both sides: the partner's manage
/// page picks its sales staff, the Admin Users page picks a Sales user's
/// partners. Reads are AdminOnly; writes require Administrator level or
/// above. Visibility-only — commission stays per student.
/// </summary>
[Route("/v1/admin/partners/{partnerId:guid}/sales-staff")]
[EndpointTag("Admin.Partners")]
public sealed class AdminV1SalesAssignmentsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partners/{partnerId:guid}/sales-staff", ListForPartnerAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partners/{partnerId:guid}/sales-staff", SetForPartnerAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/sales-staff/{userId}/partners", ListForUserAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/sales-staff/{userId}/partners", SetForUserAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> ListForPartnerAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var userIds = await db.SalesPartnerAssignments
            .Where(a => a.PartnerId == partnerId)
            .Select(a => a.UserId)
            .ToListAsync(ct);
        return Results.Ok(new { userIds });
    }

    public sealed class PartnerBody { public List<string>? UserIds { get; init; } }

    private static async Task<IResult> SetForPartnerAsync(
        Guid partnerId, [FromBody] PartnerBody body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(httpContext.User, AdminPermissions.PartnersAssignSales, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();

        var wanted = (body.UserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToHashSet();
        var existing = await db.SalesPartnerAssignments.Where(a => a.PartnerId == partnerId).ToListAsync(ct);
        db.SalesPartnerAssignments.RemoveRange(existing.Where(a => !wanted.Contains(a.UserId)));
        foreach (var id in wanted.Where(id => existing.All(a => a.UserId != id)))
            db.SalesPartnerAssignments.Add(new SalesPartnerAssignment { PartnerId = partnerId, UserId = id });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerId, count = wanted.Count });
    }

    private static async Task<IResult> ListForUserAsync(string userId, OdinDbContext db, CancellationToken ct)
    {
        var partnerIds = await db.SalesPartnerAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.PartnerId)
            .ToListAsync(ct);
        return Results.Ok(new { partnerIds });
    }

    public sealed class UserBody { public List<Guid>? PartnerIds { get; init; } }

    private static async Task<IResult> SetForUserAsync(
        string userId, [FromBody] UserBody body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(httpContext.User, AdminPermissions.PartnersAssignSales, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var wanted = (body.PartnerIds ?? []).Distinct().ToHashSet();
        var existing = await db.SalesPartnerAssignments.Where(a => a.UserId == userId).ToListAsync(ct);
        db.SalesPartnerAssignments.RemoveRange(existing.Where(a => !wanted.Contains(a.PartnerId)));
        foreach (var id in wanted.Where(id => existing.All(a => a.PartnerId != id)))
            db.SalesPartnerAssignments.Add(new SalesPartnerAssignment { PartnerId = id, UserId = userId });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { userId, count = wanted.Count });
    }
}
