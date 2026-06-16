using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

/// <summary>
/// Create a new partner-owned programme. If <c>SourceProgrammeId</c> is set,
/// the source programme (which must be granted to the caller) is deep-cloned
/// — Specializations and their Subjects are duplicated. Otherwise an empty
/// programme is created with the provided <c>Name</c>.
/// </summary>
[Route("/v1/partner/my-programs")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsCreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-programs", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class CreateRequest
    {
        public Guid? SourceProgrammeId { get; init; }
        public string? Name { get; init; }
        public int? MinDurationMonths { get; init; }
        public int? MaxDurationMonths { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateRequest body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var newProgrammeId = Guid.NewGuid();

        if (body.SourceProgrammeId is { } sourceId)
        {
            var granted = await db.ProgrammePartners
                .AnyAsync(pp => pp.PartnerId == partnerId && pp.ProgrammeId == sourceId && pp.IsActive != null, ct);
            if (!granted) return Results.BadRequest(new { error = "Source programme is not granted to your partner." });

            var source = await db.Programmes
                .Where(p => p.ProgrammeId == sourceId && p.DeletedAt == null)
                .Select(p => new { p.Name, p.Code, p.Description, p.AwardEducationLevelId, p.MinDurationMonths, p.MaxDurationMonths })
                .FirstOrDefaultAsync(ct);
            if (source is null) return Results.BadRequest(new { error = "Source programme not found." });

            db.Programmes.Add(new Programme
            {
                ProgrammeId = newProgrammeId,
                OwnerId = partnerId,
                Name = source.Name,
                Code = $"{source.Code}-{newProgrammeId.ToString()[..8]}",
                Description = source.Description,
                AwardEducationLevelId = source.AwardEducationLevelId,
                MinDurationMonths = source.MinDurationMonths,
                MaxDurationMonths = source.MaxDurationMonths,
            });

            var sourceSpecs = await db.Specializations
                .Where(s => s.ProgrammeId == sourceId && s.DeletedAt == null)
                .Select(s => new
                {
                    s.SpecializationId,
                    s.Name,
                    s.Code,
                    s.Description,
                    s.DurationOfStudyMonths,
                    Subjects = db.Subjects
                        .Where(sub => sub.SpecializationId == s.SpecializationId && sub.DeletedAt == null)
                        .Select(sub => new { sub.Name, sub.Code, sub.Description, sub.Ects })
                        .ToList(),
                })
                .ToListAsync(ct);

            // Letter templates are programme-scoped; clone them so the new
            // partner-owned programme starts with the IBSS-authored letters.
            // Admin can edit them afterwards from the partner manage drawer.
            var sourceLetters = await db.LetterTemplates
                .Where(t => t.ProgrammeId == sourceId && t.DeletedAt == null)
                .Select(t => new
                {
                    t.LetterType,
                    t.BodyHtml,
                    t.CertificateBackgroundPath,
                    t.CertificateLayoutJson,
                })
                .ToListAsync(ct);
            foreach (var l in sourceLetters)
            {
                db.LetterTemplates.Add(new LetterTemplate
                {
                    LetterTemplateId = Guid.NewGuid(),
                    ProgrammeId = newProgrammeId,
                    // Owned programme: templates belong to the creating partner.
                    PartnerId = partnerId!.Value,
                    LetterType = l.LetterType,
                    BodyHtml = l.BodyHtml,
                    CertificateBackgroundPath = l.CertificateBackgroundPath,
                    CertificateLayoutJson = l.CertificateLayoutJson,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            foreach (var s in sourceSpecs)
            {
                var newSpecId = Guid.NewGuid();
                db.Specializations.Add(new Specialization
                {
                    SpecializationId = newSpecId,
                    ProgrammeId = newProgrammeId,
                    Name = s.Name,
                    Code = $"{s.Code}-{newSpecId.ToString()[..8]}",
                    Description = s.Description,
                    DurationOfStudyMonths = s.DurationOfStudyMonths,
                });
                foreach (var sub in s.Subjects)
                {
                    db.Subjects.Add(new Subject
                    {
                        SubjectId = Guid.NewGuid(),
                        SpecializationId = newSpecId,
                        Name = sub.Name,
                        Code = sub.Code,
                        Description = sub.Description,
                        Ects = sub.Ects,
                    });
                }
            }
        }
        else
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return Results.BadRequest(new { error = "Name is required." });
            if (body.MinDurationMonths is null || body.MaxDurationMonths is null)
                return Results.BadRequest(new { error = "minDurationMonths and maxDurationMonths are required." });
            if (body.MinDurationMonths < 1 || body.MaxDurationMonths < body.MinDurationMonths)
                return Results.BadRequest(new { error = "Invalid duration range: need 1 ≤ min ≤ max." });

            db.Programmes.Add(new Programme
            {
                ProgrammeId = newProgrammeId,
                OwnerId = partnerId,
                Name = name,
                Code = $"P-{newProgrammeId.ToString()[..8]}",
                Description = string.Empty,
                MinDurationMonths = body.MinDurationMonths.Value,
                MaxDurationMonths = body.MaxDurationMonths.Value,
            });
        }

        db.PartnerProgrammeStatuses.Add(new PartnerProgrammeStatus
        {
            ProgrammeId = newProgrammeId,
            Status = MyProgramsHelpers.StatusDraft,
            IsActive = false,
            IsDisabledByAdmin = false,
            UpdatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = newProgrammeId });
    }
}
