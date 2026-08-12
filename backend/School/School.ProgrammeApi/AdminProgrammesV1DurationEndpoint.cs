using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.ProgrammeApi;

/// <summary>
/// Programme duration tools for Admission (Administrator+):
///  - PATCH the min/max range + unit even on a LOCKED programme (partners
///    still can't edit a locked programme, but admission may correct the
///    range and unit).
///  - Backfill: set each enrolled student's approved duration to the number
///    of days between their commencement and graduation dates. GET previews
///    the change list; POST applies it. Only enrolments with BOTH dates are
///    changed; others are reported as skipped.
/// </summary>
[Route("/v1/school/programmes/{programmeId:guid}/duration")]
[EndpointTag("School.Programmes")]
public sealed class AdminProgrammesV1DurationEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/school/programmes/{programmeId:guid}/duration-range", RangeAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/school/programmes/{programmeId:guid}/duration-backfill", PreviewAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/programmes/{programmeId:guid}/duration-backfill", ApplyAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class RangeBody
    {
        public int? MinDurationMonths { get; init; }
        public int? MaxDurationMonths { get; init; }
        public string? DurationRangeUnit { get; init; }
    }

    private static async Task<IResult?> RequireAdministratorAsync(
        HttpContext ctx, UserManager<ApplicationUser> um,
        IPermissionService perms, CancellationToken ct)
    {
        var callerId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await um.FindByIdAsync(callerId);
        if (caller is null) return Results.Unauthorized();
        var ok = await perms.HasAsync(ctx.User, AdminPermissions.ProgrammesEditDuration, ct);
        return ok ? null : Results.Json(new { error = "Requires Administrator level or above." },
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> RangeAsync(
        Guid programmeId, [FromBody] RangeBody body, HttpContext ctx, OdinDbContext db,
        UserManager<ApplicationUser> um, IPermissionService perms, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(ctx, um, perms, ct) is { } fail) return fail;
        var prog = await db.Programmes.FirstOrDefaultAsync(p => p.ProgrammeId == programmeId && p.DeletedAt == null, ct);
        if (prog is null) return Results.NotFound();

        var min = body.MinDurationMonths ?? prog.MinDurationMonths;
        var max = body.MaxDurationMonths ?? prog.MaxDurationMonths;
        if (min < 1 || max < min)
            return Results.BadRequest(new { error = "Invalid range: need 1 ≤ min ≤ max." });
        prog.MinDurationMonths = min;
        prog.MaxDurationMonths = max;
        if (body.DurationRangeUnit is not null)
            prog.DurationRangeUnit = string.Equals(body.DurationRangeUnit, "Day", StringComparison.OrdinalIgnoreCase) ? "Day" : "Month";
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId, min, max, unit = prog.DurationRangeUnit });
    }

    private sealed record Row(
        Guid StudentEnrollmentId, Guid StudentId, string StudentNumber, string Name,
        string ProgrammeCode, string SpecializationName,
        DateTime? Commencement, DateTime? Graduation,
        int? CurrentDays, string? CurrentUnitValue, int? NewDays, bool Skipped, string? SkipReason);

    private static async Task<List<Row>> BuildRowsAsync(OdinDbContext db, Guid programmeId, CancellationToken ct)
    {
        var rows = await db.Enrollments
            .Where(e => e.DeletedAt == null && e.Specialization.ProgrammeId == programmeId)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.CommencementDate,
                e.GraduationDate,
                e.ApprovedDurationValue,
                e.ApprovedDurationUnit,
                StudentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                Name = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId)
                        .Select(p => (p.FirstName + " " + p.LastName).Trim()).FirstOrDefault()).FirstOrDefault(),
                ProgrammeCode = e.Specialization.Programmes.Code,
                SpecializationName = e.Specialization.Name,
            })
            .ToListAsync(ct);

        return rows.Select(e =>
        {
            var currentDays = SharedLibrary.Basics.Opaque.Domains.DurationDays
                .ToDays(e.CommencementDate, e.ApprovedDurationValue, e.ApprovedDurationUnit);
            var currentLabel = e.ApprovedDurationValue is null ? null
                : SharedLibrary.Basics.Opaque.Domains.DurationDays.Display(e.ApprovedDurationValue, e.ApprovedDurationUnit);
            if (e.CommencementDate is not { } c || e.GraduationDate is not { } g)
                return new Row(e.StudentEnrollmentId, e.StudentId, e.StudentNumber ?? "?", e.Name ?? "?",
                    e.ProgrammeCode, e.SpecializationName, e.CommencementDate, e.GraduationDate,
                    currentDays, currentLabel, null, true,
                    e.CommencementDate is null ? "No commencement date" : "No graduation date");
            // Inclusive span: commencement 1 Jan → graduation 30 Jan = 30 days.
            var newDays = (int)(g.Date - c.Date).TotalDays + 1;
            if (newDays < 1) newDays = 1;
            return new Row(e.StudentEnrollmentId, e.StudentId, e.StudentNumber ?? "?", e.Name ?? "?",
                e.ProgrammeCode, e.SpecializationName, e.CommencementDate, e.GraduationDate,
                currentDays, currentLabel, newDays, false, null);
        }).OrderBy(r => r.StudentNumber).ToList();
    }

    private static object Project(Row r) => new
    {
        studentEnrollmentId = r.StudentEnrollmentId,
        studentId = r.StudentId,
        studentNumber = r.StudentNumber,
        name = r.Name,
        programmeCode = r.ProgrammeCode,
        specializationName = r.SpecializationName,
        commencement = r.Commencement,
        graduation = r.Graduation,
        currentLabel = r.CurrentUnitValue,
        currentDays = r.CurrentDays,
        newDays = r.NewDays,
        skipped = r.Skipped,
        skipReason = r.SkipReason,
    };

    private static async Task<IResult> PreviewAsync(
        Guid programmeId, HttpContext ctx, OdinDbContext db, UserManager<ApplicationUser> um, IPermissionService perms, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(ctx, um, perms, ct) is { } fail) return fail;
        if (!await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId && p.DeletedAt == null, ct))
            return Results.NotFound();
        var rows = await BuildRowsAsync(db, programmeId, ct);
        return Results.Ok(new
        {
            items = rows.Select(Project),
            willChange = rows.Count(r => !r.Skipped),
            skipped = rows.Count(r => r.Skipped),
        });
    }

    private static async Task<IResult> ApplyAsync(
        Guid programmeId, HttpContext ctx, OdinDbContext db, UserManager<ApplicationUser> um, IPermissionService perms, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(ctx, um, perms, ct) is { } fail) return fail;
        if (!await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId && p.DeletedAt == null, ct))
            return Results.NotFound();
        var rows = await BuildRowsAsync(db, programmeId, ct);
        var changeable = rows.Where(r => !r.Skipped && r.NewDays is not null).ToList();
        var ids = changeable.Select(r => r.StudentEnrollmentId).ToList();
        var enrolments = await db.Enrollments.Where(e => ids.Contains(e.StudentEnrollmentId)).ToListAsync(ct);
        foreach (var e in enrolments)
        {
            var row = changeable.First(r => r.StudentEnrollmentId == e.StudentEnrollmentId);
            e.ApprovedDurationValue = row.NewDays;
            e.ApprovedDurationUnit = SharedLibrary.Basics.Opaque.Domains.DurationDays.UnitDay;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { applied = enrolments.Count, skipped = rows.Count(r => r.Skipped) });
    }
}
