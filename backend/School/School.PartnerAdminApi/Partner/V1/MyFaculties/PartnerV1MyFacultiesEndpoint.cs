using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Admin.V1.Faculties;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Partner.V1.MyFaculties;

/// <summary>
/// Partner-portal Faculties: the partner's TEACHERS (their own users with
/// the Teacher role) and their profiles shaped by the global Faculty Profile
/// Information structure. Partners fill only the fields flagged
/// PartnerCanEdit; MGW-only fields show locked; rows append, never delete.
/// </summary>
[Route("/v1/partner/my/teachers")]
[EndpointTag("Partner.MyFaculties")]
public sealed class PartnerV1MyFacultiesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/teachers", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/teachers", CreateAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/teachers/{teacherId:guid}", GetProfileAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/my/teachers/{teacherId:guid}", SaveProfileAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/faculty-files", UploadFileAsync).RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapGet("/v1/partner/my/faculty-values/{valueId:guid}/file", DownloadFileAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/faculty-values/{valueId:guid}/file/{index:int}", DownloadFilesItemAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var items = await db.Teachers
            .Where(t => t.PartnerId == partnerId && t.DeletedAt == null)
            .OrderBy(t => t.DisplayName)
            .Select(t => new
            {
                teacherId = t.TeacherId,
                displayName = t.DisplayName,
                userId = t.UserId,
                username = db.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName).FirstOrDefault(),
                email = db.Users.Where(u => u.Id == t.UserId).Select(u => u.Email).FirstOrDefault(),
                updatedAt = t.UpdatedAt ?? t.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, [FromBody] AdminV1FacultiesEndpoint.CreateTeacherBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        if (string.IsNullOrWhiteSpace(body.DisplayName))
            return Results.BadRequest(new { error = "Teacher name is required." });

        // A login is OPTIONAL — a teacher can exist without any user account.
        string? userId = null;
        if (!string.IsNullOrWhiteSpace(body.TeacherUserId))
        {
            var user = await db.Users
                .Where(u => u.Id == body.TeacherUserId && u.PartnerId == partnerId && u.DeletedAt == null)
                .Select(u => new { u.Id, u.IsTeacher })
                .FirstOrDefaultAsync(ct);
            if (user is null) return Results.BadRequest(new { error = "Teacher user not found." });
            if (!user.IsTeacher) return Results.BadRequest(new { error = "That user is not a Teacher." });
            var taken = await db.Teachers.AnyAsync(t => t.UserId == user.Id && t.DeletedAt == null, ct);
            if (taken) return Results.Conflict(new { error = "That user already has a teacher record." });
            userId = user.Id;
        }

        var teacher = new Teacher
        {
            PartnerId = partnerId.Value,
            UserId = userId,
            DisplayName = body.DisplayName.Trim(),
        };
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { teacherId = teacher.TeacherId });
    }

    private static async Task<(Guid? PartnerId, bool Owned)> ResolveTeacherAsync(
        HttpContext httpContext, Guid teacherId, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return (null, false);
        var owned = await db.Teachers.AnyAsync(t =>
            t.TeacherId == teacherId && t.PartnerId == partnerId && t.DeletedAt == null, ct);
        return (partnerId, owned);
    }

    private static async Task<IResult> GetProfileAsync(
        Guid teacherId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, owned) = await ResolveTeacherAsync(httpContext, teacherId, db, ct);
        if (!owned) return Results.NotFound();
        var profile = await FacultyProfileLogic.ProfileAsync(db, teacherId, ct);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> SaveProfileAsync(
        Guid teacherId, HttpContext httpContext,
        [FromBody] FacultyProfileLogic.SaveBody body, OdinDbContext db, CancellationToken ct)
    {
        var (callerId, _, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var (_, owned) = await ResolveTeacherAsync(httpContext, teacherId, db, ct);
        if (!owned) return Results.NotFound();
        // Teacher users may edit ONLY their own profile (the write-gate lets
        // the PUT through; ownership is enforced here).
        var callerIsTeacher = await db.Users.AnyAsync(u => u.Id == callerId && u.IsTeacher, ct);
        if (callerIsTeacher)
        {
            var own = await db.Teachers.AnyAsync(t =>
                t.TeacherId == teacherId && t.UserId == callerId && t.DeletedAt == null, ct);
            if (!own) return Results.StatusCode(403);
        }
        var ok = await FacultyProfileLogic.SaveProfileAsync(db, teacherId, body, partnerEditableOnly: true, ct);
        return ok ? Results.Ok(new { saved = true }) : Results.NotFound();
    }

    private static async Task<IResult> UploadFileAsync(
        HttpContext httpContext, IFormFile file, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, _, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > 50 * 1024 * 1024)
            return Results.BadRequest(new { error = "Max file size is 50 MB." });

        var safeName = Path.GetFileName(file.FileName);
        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(stream, $"{FacultyProfileLogic.StoragePrefix}{Guid.NewGuid():N}-{safeName}", ct);
        }
        return Results.Ok(new { token = storagePath, fileName = safeName });
    }

    private static async Task<IResult> DownloadFileAsync(
        Guid valueId, HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var value = await (
            from v in db.TeacherProfileValues
            join r in db.TeacherProfileRows on v.TeacherProfileRowId equals r.TeacherProfileRowId
            join t in db.Teachers on r.TeacherId equals t.TeacherId
            where v.TeacherProfileValueId == valueId && t.PartnerId == partnerId && t.DeletedAt == null
            select new { v.Value, v.FileName }).FirstOrDefaultAsync(ct);
        if (value is null || !value.Value.Contains(FacultyProfileLogic.StoragePrefix)) return Results.NotFound();

        try
        {
            var stream = await storage.OpenReadAsync(value.Value, ct);
            return Results.File(stream, "application/octet-stream", value.FileName ?? "faculty-file");
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }

    /// <summary>
    /// Streams one file of a "files" cell (JSON array of {token,name}) at
    /// {index}, under the same partner-ownership guard as the single-file
    /// download.
    /// </summary>
    private static async Task<IResult> DownloadFilesItemAsync(
        Guid valueId, int index, HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var value = await (
            from v in db.TeacherProfileValues
            join r in db.TeacherProfileRows on v.TeacherProfileRowId equals r.TeacherProfileRowId
            join t in db.Teachers on r.TeacherId equals t.TeacherId
            where v.TeacherProfileValueId == valueId && t.PartnerId == partnerId && t.DeletedAt == null
            select new { v.Value }).FirstOrDefaultAsync(ct);
        if (value is null) return Results.NotFound();

        var item = FacultyProfileLogic.FilesItemAt(value.Value, index);
        if (item is null || !item.Token.Contains(FacultyProfileLogic.StoragePrefix)) return Results.NotFound();

        try
        {
            var stream = await storage.OpenReadAsync(item.Token, ct);
            return Results.File(stream, "application/octet-stream", item.Name ?? "faculty-file");
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
