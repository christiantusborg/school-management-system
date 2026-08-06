using System.Security.Claims;
using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Admin.V1.Letters;

/// <summary>
/// Version history of released letters (config-created letter types record a
/// StudentDocumentVersion per render; built-in letters join at migration).
/// Everyone may browse history per the agreed spec: these are the admin and
/// partner routes; the student route lives in the StudentApi. The live
/// document keeps serving the latest PDF — versions are read-only history.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{documentId:guid}/versions")]
[EndpointTag("Admin.Letters")]
public sealed class LetterVersionsEndpoints : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{documentId:guid}/versions",
            AdminListAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{documentId:guid}/versions/{versionId:guid}/file",
            AdminFileAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{documentId:guid}/versions",
            PartnerListAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/letters/{documentId:guid}/versions/{versionId:guid}/file",
            PartnerFileAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<bool> DocBelongsAsync(
        OdinDbContext db, Guid studentId, Guid enrollmentId, Guid documentId, CancellationToken ct) =>
        await db.StudentDocuments.AnyAsync(d => d.StudentDocumentId == documentId
            && d.StudentId == studentId && d.EnrollmentId == enrollmentId && d.DeletedAt == null, ct);

    private static async Task<object> VersionRowsAsync(OdinDbContext db, Guid documentId, CancellationToken ct)
    {
        var items = await db.StudentDocumentVersions
            .Where(v => v.StudentDocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new
            {
                studentDocumentVersionId = v.StudentDocumentVersionId,
                versionNumber = v.VersionNumber,
                fileName = v.FileName,
                trigger = v.Trigger,
                generatedByName = v.GeneratedByName,
                language = v.Language,
                createdAt = v.CreatedAt,
            })
            .ToListAsync(ct);
        return new { items };
    }

    private static async Task<IResult> StreamVersionAsync(
        OdinDbContext db, IFileStorage storage, Guid documentId, Guid versionId, CancellationToken ct)
    {
        var v = await db.StudentDocumentVersions
            .Where(x => x.StudentDocumentVersionId == versionId && x.StudentDocumentId == documentId)
            .Select(x => new { x.StoragePath, x.FileName })
            .FirstOrDefaultAsync(ct);
        if (v is null) return Results.NotFound();
        var stream = await storage.OpenReadAsync(v.StoragePath, ct);
        return Results.File(stream, "application/pdf", v.FileName);
    }

    private static async Task<IResult> AdminListAsync(
        Guid studentId, Guid enrollmentId, Guid documentId, OdinDbContext db, CancellationToken ct)
    {
        if (!await DocBelongsAsync(db, studentId, enrollmentId, documentId, ct)) return Results.NotFound();
        return Results.Ok(await VersionRowsAsync(db, documentId, ct));
    }

    private static async Task<IResult> AdminFileAsync(
        Guid studentId, Guid enrollmentId, Guid documentId, Guid versionId,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        if (!await DocBelongsAsync(db, studentId, enrollmentId, documentId, ct)) return Results.NotFound();
        return await StreamVersionAsync(db, storage, documentId, versionId, ct);
    }

    private static async Task<(bool Ok, Guid DocTypeId)> PartnerOwnsDocAsync(
        HttpContext http, OdinDbContext db, Guid studentId, Guid enrollmentId, Guid documentId, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(http, db, ct);
        if (fail is not null) return (false, Guid.Empty);
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        if (!owns) return (false, Guid.Empty);
        var docTypeId = await db.StudentDocuments
            .Where(d => d.StudentDocumentId == documentId
                && d.StudentId == studentId && d.EnrollmentId == enrollmentId && d.DeletedAt == null)
            .Select(d => (Guid?)d.DocumentTypeId)
            .FirstOrDefaultAsync(ct);
        if (docTypeId is null) return (false, Guid.Empty);
        // Config-created letters honour the type's partner-visibility switch.
        var hiddenFromPartner = await db.LetterTypeDefinitions.AnyAsync(d =>
            d.DocumentTypeId == docTypeId && d.DeletedAt == null && !d.VisibleToPartner, ct);
        return (!hiddenFromPartner, docTypeId.Value);
    }

    private static async Task<IResult> PartnerListAsync(
        Guid studentId, Guid enrollmentId, Guid documentId, HttpContext http,
        OdinDbContext db, CancellationToken ct)
    {
        var (ok, _) = await PartnerOwnsDocAsync(http, db, studentId, enrollmentId, documentId, ct);
        if (!ok) return Results.NotFound();
        return Results.Ok(await VersionRowsAsync(db, documentId, ct));
    }

    private static async Task<IResult> PartnerFileAsync(
        Guid studentId, Guid enrollmentId, Guid documentId, Guid versionId, HttpContext http,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (ok, _) = await PartnerOwnsDocAsync(http, db, studentId, enrollmentId, documentId, ct);
        if (!ok) return Results.NotFound();
        return await StreamVersionAsync(db, storage, documentId, versionId, ct);
    }
}
