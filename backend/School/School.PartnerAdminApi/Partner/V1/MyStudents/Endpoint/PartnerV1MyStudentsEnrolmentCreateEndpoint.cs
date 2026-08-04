using System.Security.Claims;
using Odin.Api.Base.Programmes;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner enrols one of their OWN students into an additional programme,
/// mirroring the Admission drawer's "+ Add programme". Availability is the
/// partner's actual access: core specializations granted per-spec (and not
/// switched off by the partner) plus APPROVED specializations of the
/// partner's own custom programmes. The new enrolment starts at Awaiting
/// Review by Admission so it enters the normal pipeline.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsEnrolmentCreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/enrolment-options", OptionsAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my-students/{studentId:guid}/enrollments", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class CreateBody
    {
        public Guid? SpecializationId { get; init; }
        public int ModeOfStudyId { get; init; } = 1;
    }

    /// <summary>Programme → specializations the partner may enrol into.</summary>
    private static async Task<IResult> OptionsAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var granted = await db.SpecializationPartners
            .Where(g => g.PartnerId == partnerId && !g.DisabledByPartner
                && g.Specialization.DeletedAt == null
                && g.Specialization.Programmes.DeletedAt == null)
            .Select(g => new
            {
                specializationId = g.SpecializationId,
                specializationName = g.Specialization.Name,
                programmeId = g.Specialization.ProgrammeId,
                programmeName = g.Specialization.Programmes.Name,
                programmeCode = g.Specialization.Programmes.Code,
                schoolName = g.Specialization.Programmes.School != null
                    ? g.Specialization.Programmes.School.Name : null,
            })
            .ToListAsync(ct);

        var ownApproved = await db.PartnerSpecializationStatuses
            .Where(x => x.Status == SpecApproval.StatusApproved
                && x.Specialization.DeletedAt == null
                && x.Specialization.Programmes.OwnerId == partnerId
                && x.Specialization.Programmes.DeletedAt == null)
            .Select(x => new
            {
                specializationId = x.SpecializationId,
                specializationName = x.Specialization.Name,
                programmeId = x.Specialization.ProgrammeId,
                programmeName = x.Specialization.Programmes.Name,
                programmeCode = x.Specialization.Programmes.Code,
                schoolName = x.Specialization.Programmes.School != null
                    ? x.Specialization.Programmes.School.Name : null,
            })
            .ToListAsync(ct);

        var items = granted.Concat(ownApproved)
            .GroupBy(x => x.programmeId)
            .Select(g => new
            {
                programmeId = g.Key,
                name = g.First().programmeName,
                code = g.First().programmeCode,
                schoolName = g.First().schoolName,
                specializations = g
                    .Select(x => new { x.specializationId, name = x.specializationName })
                    .DistinctBy(x => x.specializationId)
                    .OrderBy(x => x.name)
                    .ToList(),
            })
            .OrderBy(x => x.name)
            .ToList();

        return Results.Ok(new { items });
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, [FromBody] CreateBody body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);

        if (body.SpecializationId is not { } specId)
            return Results.BadRequest(new { error = "specializationId is required." });

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();

        // Availability: granted-and-enabled core spec, or an approved spec of
        // the partner's own custom programme.
        var available = await db.SpecializationPartners.AnyAsync(g =>
                g.SpecializationId == specId && g.PartnerId == partnerId && !g.DisabledByPartner, ct)
            || await db.PartnerSpecializationStatuses.AnyAsync(x =>
                x.SpecializationId == specId && x.Status == SpecApproval.StatusApproved
                && x.Specialization.Programmes.OwnerId == partnerId, ct);
        if (!available)
            return Results.BadRequest(new { error = "That programme isn't available to your partner." });

        var duplicate = await db.Enrollments.AnyAsync(e =>
            e.StudentId == studentId && e.SpecializationId == specId && e.DeletedAt == null, ct);
        if (duplicate)
            return Results.BadRequest(new { error = "The student is already enrolled in this specialization." });

        var enrollmentId = Guid.NewGuid();
        db.Enrollments.Add(new SharedLibrary.Basics.Opaque.Domains.Enrollment
        {
            StudentEnrollmentId = enrollmentId,
            StudentId = studentId,
            PartnerId = partnerId.Value,
            SpecializationId = specId,
            ModeOfStudyId = body.ModeOfStudyId,
            PathwayId = 0,
            StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
        });
        db.Set<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote>().Add(
            new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission,
                Note = "Programme added by partner.",
                ByUserId = Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enrollmentId });
    }
}
