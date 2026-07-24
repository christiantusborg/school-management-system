using Odin.Api.Base.Letters;
using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// PATCH the personal section of any student. No admission gate — IBSS
/// retains edit rights through the entire workflow.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/personal")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsPersonalEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/personal", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] StudentEditService.PersonalDto body,
        OdinDbContext db,
        LetterReleaseService letterRelease,
        CancellationToken ct)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        var oldCardId = student.StudentCardId;
        await StudentEditService.UpdatePersonalAsync(db, student, body, "admin", ct);

        // A changed card ID must show on already-issued cards: re-render every
        // released Student ID Card of this student's enrolments (best effort).
        if (!string.Equals(oldCardId ?? "", student.StudentCardId ?? "", StringComparison.Ordinal))
        {
            var cardEnrollments = await db.StudentDocuments
                .Where(d => d.StudentId == student.StudentId
                    && d.DocumentTypeId == SystemDocumentTypeIds.StudentIdCard
                    && d.DeletedAt == null && d.EnrollmentId != null)
                .Select(d => d.EnrollmentId!.Value)
                .Distinct()
                .ToListAsync(ct);
            foreach (var enrollmentId in cardEnrollments)
            {
                try { await letterRelease.ReleaseAsync(enrollmentId, LetterType.StudentIdCard, ct); }
                catch { /* keep the personal save even if a re-render fails */ }
            }
        }

        return Results.NoContent();
    }
}
