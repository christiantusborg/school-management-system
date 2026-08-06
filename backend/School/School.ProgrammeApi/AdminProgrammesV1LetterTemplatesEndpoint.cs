using Odin.Api.Base.Letters;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

[Route("/v1/admin/programmes")]
[EndpointTag("Admin.LetterTemplates")]
public sealed class AdminProgrammesV1LetterTemplatesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/programmes/{programmeId:guid}/letter-templates", ListAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/programmes/{programmeId:guid}/letter-templates/{type}", UpsertAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/letter-tags", ListTagsAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteRequest
    {
        public string? BodyHtml { get; init; }
        public string? CertificateBackgroundPath { get; init; }
        public string? CertificateLayoutJson { get; init; }
    }

    private static async Task<IResult> ListAsync(
        OdinDbContext db, Guid programmeId, CancellationToken ct,
        [FromQuery] Guid? partnerId = null)
    {
        var programmeExists = await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId, ct);
        if (!programmeExists) return Results.NotFound(new { message = "programme not found" });

        // Templates are per (programme, partner, type). The editor always scopes
        // to a selected partner; without one, return nothing rather than mixing
        // partners.
        if (partnerId is null) return Results.Ok(new { items = Array.Empty<object>(), total = 0 });

        var rows = await db.LetterTemplates
            .Where(t => t.ProgrammeId == programmeId && t.PartnerId == partnerId && t.DeletedAt == null)
            .Select(t => new
            {
                letterTemplateId = t.LetterTemplateId,
                programmeId = t.ProgrammeId,
                partnerId = t.PartnerId,
                // Dynamic (config-created) templates carry a definition id and
                // report no enum letterType, so the built-in publish badges
                // never get polluted by a definition row (whose enum column
                // sits at its default value).
                letterType = t.LetterTypeDefinitionId == null ? t.LetterType.ToString() : null,
                letterTypeDefinitionId = t.LetterTypeDefinitionId,
                language = t.Language,
                bodyHtml = t.BodyHtml,
                certificateBackgroundPath = t.CertificateBackgroundPath,
                certificateLayoutJson = t.CertificateLayoutJson,
                isPublished = t.IsPublished,
                updatedAt = t.UpdatedAt,
                updatedByUserId = t.UpdatedByUserId,
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
        [FromQuery] Guid? partnerId = null,
        [FromQuery] string? language = null)
    {
        // The route's {type} is either a built-in enum name (OfferLetter…) or
        // the GUID of a config-created LetterTypeDefinition.
        LetterType letterType = default;
        Guid? definitionId = null;
        if (Guid.TryParse(type, out var parsedDefId))
        {
            var defExists = await db.LetterTypeDefinitions
                .AnyAsync(d => d.LetterTypeDefinitionId == parsedDefId && d.DeletedAt == null, ct);
            if (!defExists) return Results.BadRequest(new { message = "unknown letter type" });
            definitionId = parsedDefId;
        }
        else if (!Enum.TryParse(type, ignoreCase: true, out letterType))
        {
            return Results.BadRequest(new { message = "unknown letter type" });
        }
        if (partnerId is null) return Results.BadRequest(new { message = "partnerId is required" });

        // Language versions: null/blank = the English default; anything else
        // must be on the configured letter-language list.
        var lang = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
        if (lang is not null && !await db.LetterLanguages.AnyAsync(l => l.Name == lang && l.DeletedAt == null, ct))
            return Results.BadRequest(new { message = $"language '{lang}' is not on the letter-language list" });

        var programmeExists = await db.Programmes.AnyAsync(p => p.ProgrammeId == programmeId, ct);
        if (!programmeExists) return Results.NotFound(new { message = "programme not found" });

        var entity = await db.LetterTemplates.FirstOrDefaultAsync(t =>
            t.ProgrammeId == programmeId &&
            t.PartnerId == partnerId &&
            (definitionId == null
                ? t.LetterTypeDefinitionId == null && t.LetterType == letterType
                : t.LetterTypeDefinitionId == definitionId) &&
            t.Language == lang &&
            t.DeletedAt == null, ct);

        if (entity is null)
        {
            entity = new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = programmeId,
                PartnerId = partnerId.Value,
                LetterType = letterType,
                LetterTypeDefinitionId = definitionId,
                Language = lang,
            };
            db.LetterTemplates.Add(entity);
        }

        if (body.BodyHtml is not null)                  entity.BodyHtml = body.BodyHtml;
        if (body.CertificateBackgroundPath is not null) entity.CertificateBackgroundPath = body.CertificateBackgroundPath;
        if (body.CertificateLayoutJson is not null)     entity.CertificateLayoutJson = body.CertificateLayoutJson;
        // Saving from the editor publishes the template — release service
        // will start rendering PDFs for this (programme, letter type).
        entity.IsPublished = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { letterTemplateId = entity.LetterTemplateId, isPublished = entity.IsPublished });
    }

    private static IResult ListTagsAsync()
        => Results.Ok(new
        {
            items = LetterTagRegistry.All.Select(t => new { token = t.Token, key = t.Key }).ToList(),
            total = LetterTagRegistry.All.Count,
        });
}
