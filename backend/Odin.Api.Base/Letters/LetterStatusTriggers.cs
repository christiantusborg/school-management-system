using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Fires the config-created letter types whose TriggerStatusId matches a
/// status an enrolment just reached. Fire-once semantics: a type that already
/// has a released document for the enrolment is skipped, so bouncing in and
/// out of a status never spams versions (regenerate manually instead).
/// Best-effort by design — a failed render logs and never breaks the status
/// change that triggered it. Call AFTER the status change has been saved.
/// </summary>
public static class LetterStatusTriggers
{
    public static async Task FireAsync(
        OdinDbContext db, LetterReleaseService letterRelease, ILogger logger,
        Guid enrollmentId, Guid statusId, CancellationToken ct)
    {
        try
        {
            var definitions = await db.LetterTypeDefinitions
                .Where(d => d.TriggerStatusId == statusId && d.DeletedAt == null)
                .Select(d => new { d.LetterTypeDefinitionId, d.DocumentTypeId, d.Name })
                .ToListAsync(ct);
            foreach (var def in definitions)
            {
                var alreadyReleased = await db.StudentDocuments.AnyAsync(x =>
                    x.EnrollmentId == enrollmentId
                    && x.DocumentTypeId == def.DocumentTypeId
                    && x.DeletedAt == null, ct);
                if (alreadyReleased) continue;
                var docId = await letterRelease.ReleaseDynamicAsync(
                    enrollmentId, def.LetterTypeDefinitionId, language: null,
                    trigger: "StatusTrigger", generatedByName: null, generatedByUserId: null,
                    letterTypeHint: null, ct);
                if (docId is not null)
                    logger.LogInformation("[Letters] Status trigger released '{Name}' for enrolment {EnrollmentId}",
                        def.Name, enrollmentId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Letters] Status trigger failed for enrolment {EnrollmentId} status {StatusId}",
                enrollmentId, statusId);
        }
    }
}
