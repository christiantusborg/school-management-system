using School.PartnerAdminApi.Partner.V1.MyUsers;
using Odin.Api.Base.Letters;
using School.PartnerAdminApi.SharedStudentEdits;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// PATCH the personal section of a student the partner owns. Blocked
/// once any of the student's enrollments has reached
/// <c>ApplicationApprovedAdmission</c> or later — at that point IBSS
/// owns the identity record.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/personal")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsPersonalEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/partner/my-students/{studentId:guid}/personal", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] StudentEditService.PersonalDto body,
        HttpContext httpContext,
        OdinDbContext db,
        LetterReleaseService letterRelease,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId
                && s.PartnerId == partnerId
                && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        if (await StudentEditService.IsAdmittedAsync(db, studentId, ct))
            return Results.BadRequest(new { error = "Student is admitted; profile is read-only." });

        var oldCardId = student.StudentCardId;
        await StudentEditService.UpdatePersonalAsync(db, student, body, "partner", ct);

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
