using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Programmes;

/// <summary>
/// Specialization-level approval workflow for partner-owned programmes.
/// The programme's own <see cref="PartnerProgrammeStatus"/> is DERIVED from
/// its specialization statuses: a programme is Approved (and goes live) as
/// soon as one spec is approved, Pending while any spec awaits review,
/// Rejected when every reviewed spec was rejected, Draft otherwise.
/// Admin's programme-level Disable stays authoritative as a kill switch.
/// </summary>
public static class SpecApproval
{
    public const int StatusDraft = 0;
    public const int StatusPending = 1;
    public const int StatusApproved = 2;
    public const int StatusRejected = 3;

    public static string Label(int s) => s switch
    {
        StatusDraft => "Draft",
        StatusPending => "Pending",
        StatusApproved => "Approved",
        StatusRejected => "Rejected",
        _ => "Draft",
    };

    /// <summary>Ensure a status row exists for a partner-owned spec.</summary>
    public static async Task<PartnerSpecializationStatus> EnsureAsync(
        OdinDbContext db, Guid specializationId, CancellationToken ct)
    {
        var row = await db.PartnerSpecializationStatuses
            .FirstOrDefaultAsync(s => s.SpecializationId == specializationId, ct);
        if (row is null)
        {
            row = new PartnerSpecializationStatus
            {
                SpecializationId = specializationId,
                Status = StatusDraft,
                UpdatedAt = DateTime.UtcNow,
            };
            db.PartnerSpecializationStatuses.Add(row);
        }
        return row;
    }

    /// <summary>
    /// Recompute the derived programme row after any spec-status change.
    /// Reads committed rows, so CALL THIS AFTER SaveChangesAsync of the spec
    /// change; it saves the programme row itself. First approval
    /// auto-activates the programme (unless admin-disabled); losing the last
    /// approved spec deactivates it.
    /// </summary>
    public static async Task RecomputeProgrammeAsync(
        OdinDbContext db, Guid programmeId, CancellationToken ct)
    {
        var specStatuses = await db.PartnerSpecializationStatuses
            .Where(x => x.Specialization.ProgrammeId == programmeId
                && x.Specialization.DeletedAt == null)
            .Select(x => x.Status)
            .ToListAsync(ct);

        var derived =
            specStatuses.Any(s => s == StatusApproved) ? StatusApproved
            : specStatuses.Any(s => s == StatusPending) ? StatusPending
            : specStatuses.Any(s => s == StatusRejected) && specStatuses.All(s => s is StatusRejected) ? StatusRejected
            : StatusDraft;

        var prog = await db.PartnerProgrammeStatuses
            .FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (prog is null)
        {
            prog = new PartnerProgrammeStatus
            {
                ProgrammeId = programmeId,
                Status = derived,
                IsActive = false,
                IsDisabledByAdmin = false,
                UpdatedAt = DateTime.UtcNow,
            };
            db.PartnerProgrammeStatuses.Add(prog);
        }

        var wasApproved = prog.Status == StatusApproved;
        prog.Status = derived;
        if (derived == StatusApproved)
        {
            // Auto-activate on FIRST approval only; afterwards the partner's
            // manual active toggle is respected.
            if (!wasApproved) prog.IsActive = !prog.IsDisabledByAdmin;
        }
        else
        {
            prog.IsActive = false;
        }
        prog.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Valid clone sources for a custom programme: its own specs plus specs
    /// of any CORE programme sharing the same award education level.
    /// </summary>
    public static async Task<bool> IsValidCloneSourceAsync(
        OdinDbContext db, Guid targetProgrammeId, Guid sourceSpecializationId, CancellationToken ct)
    {
        var target = await db.Programmes
            .Where(p => p.ProgrammeId == targetProgrammeId)
            .Select(p => new { p.AwardEducationLevelId })
            .FirstOrDefaultAsync(ct);
        if (target is null) return false;

        return await db.Specializations
            .AnyAsync(s => s.SpecializationId == sourceSpecializationId && s.DeletedAt == null
                && (s.ProgrammeId == targetProgrammeId
                    || (s.Programmes.OwnerId == null && s.Programmes.DeletedAt == null
                        && s.Programmes.AwardEducationLevelId == target.AwardEducationLevelId)), ct);
    }

    /// <summary>Deep-clone a specialization (subjects included) into the
    /// target programme. Caller adds the status row and saves.</summary>
    public static async Task<Guid> CloneSpecializationAsync(
        OdinDbContext db, Guid targetProgrammeId, Guid sourceSpecializationId, CancellationToken ct)
    {
        var src = await db.Specializations
            .Where(s => s.SpecializationId == sourceSpecializationId && s.DeletedAt == null)
            .Select(s => new
            {
                s.Name, s.Code, s.Description, s.DurationOfStudyMonths,
                s.InstructionLanguage, s.TuitionFeeUsd, s.OfferAcceptanceMode,
                Subjects = db.Subjects
                    .Where(sub => sub.SpecializationId == s.SpecializationId && sub.DeletedAt == null)
                    .Select(sub => new { sub.Name, sub.Code, sub.Description, sub.Ects, sub.IsThesis, sub.RubricTemplateId })
                    .ToList(),
            })
            .FirstAsync(ct);

        var newSpecId = Guid.NewGuid();
        db.Specializations.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Specialization
        {
            SpecializationId = newSpecId,
            ProgrammeId = targetProgrammeId,
            Name = src.Name,
            Code = $"{src.Code}-{newSpecId.ToString()[..8]}",
            Description = src.Description,
            DurationOfStudyMonths = src.DurationOfStudyMonths,
            InstructionLanguage = src.InstructionLanguage,
            TuitionFeeUsd = src.TuitionFeeUsd,
            OfferAcceptanceMode = src.OfferAcceptanceMode,
        });
        foreach (var sub in src.Subjects)
        {
            db.Subjects.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Subject
            {
                SubjectId = Guid.NewGuid(),
                SpecializationId = newSpecId,
                Name = sub.Name,
                Code = sub.Code,
                Description = sub.Description,
                Ects = sub.Ects,
                IsThesis = sub.IsThesis,
                RubricTemplateId = sub.RubricTemplateId,
            });
        }
        return newSpecId;
    }
}
