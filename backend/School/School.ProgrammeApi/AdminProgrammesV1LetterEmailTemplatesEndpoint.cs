using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

/// <summary>
/// CRUD for the email that accompanies a released letter, edited alongside the
/// letter template. Only Offer and Admission letters are emailable; other
/// types are rejected. Saving does NOT enable sending — the admin flips
/// <c>isEmailEnabled</c> explicitly so nothing leaves by accident.
/// </summary>
[Route("/v1/admin/programmes")]
[EndpointTag("Admin.LetterTemplates")]
public sealed class AdminProgrammesV1LetterEmailTemplatesEndpoint : IEndpointMarker
{
    private static readonly HashSet<LetterType> Emailable = [LetterType.OfferLetter, LetterType.AdmissionLetter];

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/programmes/{programmeId:guid}/letter-email-templates", ListAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/programmes/{programmeId:guid}/letter-email-templates/{type}", UpsertAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteRequest
    {
        public bool? IsEmailEnabled { get; init; }
        public string? Subject { get; init; }
        public string? BodyHtml { get; init; }
        public string? CcRecipientsJson { get; init; }
        public string? BccRecipientsJson { get; init; }
    }

    private static async Task<IResult> ListAsync(
        OdinDbContext db, Guid programmeId, CancellationToken ct,
        [FromQuery] Guid? partnerId = null)
    {
        var programmeExists = await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId, ct);
        if (!programmeExists) return Results.NotFound(new { message = "programme not found" });

        // Per (programme, partner, type); the editor always scopes to a partner.
        if (partnerId is null) return Results.Ok(new { items = Array.Empty<object>(), total = 0 });

        var rows = await db.LetterEmailTemplates
            .Where(t => t.ProgrammeId == programmeId && t.PartnerId == partnerId && t.DeletedAt == null)
            .Select(t => new
            {
                letterEmailTemplateId = t.LetterEmailTemplateId,
                programmeId = t.ProgrammeId,
                partnerId = t.PartnerId,
                letterType = t.LetterType.ToString(),
                isEmailEnabled = t.IsEmailEnabled,
                subject = t.Subject,
                bodyHtml = t.BodyHtml,
                ccRecipientsJson = t.CcRecipientsJson,
                bccRecipientsJson = t.BccRecipientsJson,
                updatedAt = t.UpdatedAt,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items = rows, total = rows.Count });
    }

    private static async Task<IResult> UpsertAsync(
        OdinDbContext db,
        Guid programmeId,
        string type,
        [FromBody] WriteRequest body,
        CancellationToken ct,
        [FromQuery] Guid? partnerId = null)
    {
        if (!Enum.TryParse<LetterType>(type, ignoreCase: true, out var letterType))
            return Results.BadRequest(new { message = "unknown letter type" });
        if (!Emailable.Contains(letterType))
            return Results.BadRequest(new { message = "only Offer and Admission letters support email" });
        if (partnerId is null) return Results.BadRequest(new { message = "partnerId is required" });

        var programmeExists = await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId, ct);
        if (!programmeExists) return Results.NotFound(new { message = "programme not found" });

        var entity = await db.LetterEmailTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == programmeId &&
            t.PartnerId == partnerId &&
            t.LetterType == letterType &&
            t.DeletedAt == null, ct);

        if (entity is null)
        {
            entity = new LetterEmailTemplate
            {
                LetterEmailTemplateId = Guid.NewGuid(),
                ProgrammeId = programmeId,
                PartnerId = partnerId.Value,
                LetterType = letterType,
            };
            db.LetterEmailTemplates.Add(entity);
        }

        if (body.IsEmailEnabled is not null)    entity.IsEmailEnabled = body.IsEmailEnabled.Value;
        if (body.Subject is not null)           entity.Subject = body.Subject;
        if (body.BodyHtml is not null)          entity.BodyHtml = body.BodyHtml;
        if (body.CcRecipientsJson is not null)  entity.CcRecipientsJson = body.CcRecipientsJson;
        if (body.BccRecipientsJson is not null) entity.BccRecipientsJson = body.BccRecipientsJson;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { letterEmailTemplateId = entity.LetterEmailTemplateId, isEmailEnabled = entity.IsEmailEnabled });
    }
}
