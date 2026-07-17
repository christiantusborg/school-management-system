using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Documents;

/// <summary>
/// Resolves per-module start AND end dates for an enrolment. Resolution
/// order for each date:
///   1. the student's override (explicit date or commencement + N days)
///   2. the module's programme-level default offset (Subject.DefaultStart/
///      EndOffsetDays — applies automatically to everyone, including
///      students who sign up later)
///   3. start: the enrolment's commencement date; end: none (TBC).
/// Shared by the admin (read/write), partner (read) and student (read)
/// endpoints.
/// </summary>
public static class ModuleStartService
{
    public static async Task<object?> ListAsync(OdinDbContext db, Guid enrollmentId, CancellationToken ct)
    {
        var enr = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.DeletedAt == null)
            .Select(e => new { e.SpecializationId, e.CommencementDate })
            .FirstOrDefaultAsync(ct);
        if (enr is null) return null;

        var subjects = await db.Subjects
            .Where(s => s.SpecializationId == enr.SpecializationId && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new { s.SubjectId, s.Code, s.Name, s.DefaultStartOffsetDays, s.DefaultEndOffsetDays })
            .ToListAsync(ct);

        var overrides = await db.EnrollmentModuleStarts
            .Where(m => m.StudentEnrollmentId == enrollmentId)
            .ToDictionaryAsync(m => m.SubjectId, ct);

        var commencement = enr.CommencementDate;
        return new
        {
            commencementDate = commencement,
            modules = subjects.Select(s =>
            {
                overrides.TryGetValue(s.SubjectId, out var o);
                var hasStartOverride = o is not null
                    && ((o.UseOffset && o.OffsetDays != null) || (!o.UseOffset && o.StartDate != null));
                var hasEndOverride = o is not null
                    && ((o.EndUseOffset && o.EndOffsetDays != null) || (!o.EndUseOffset && o.EndDate != null));

                DateTime? resolvedStart = hasStartOverride
                    ? (o!.UseOffset ? commencement?.AddDays(o.OffsetDays!.Value) : o.StartDate)
                    : s.DefaultStartOffsetDays is { } dso
                        ? commencement?.AddDays(dso)
                        : commencement;
                DateTime? resolvedEnd = hasEndOverride
                    ? (o!.EndUseOffset ? commencement?.AddDays(o.EndOffsetDays!.Value) : o.EndDate)
                    : s.DefaultEndOffsetDays is { } deo
                        ? commencement?.AddDays(deo)
                        : null;

                return new
                {
                    subjectId = s.SubjectId,
                    code = s.Code,
                    name = s.Name,
                    defaultStartOffsetDays = s.DefaultStartOffsetDays,
                    defaultEndOffsetDays = s.DefaultEndOffsetDays,
                    hasOverride = hasStartOverride,
                    useOffset = o?.UseOffset ?? false,
                    startDate = hasStartOverride ? o!.StartDate : null,
                    offsetDays = hasStartOverride ? o!.OffsetDays : null,
                    resolvedDate = resolvedStart,
                    hasEndOverride,
                    endUseOffset = o?.EndUseOffset ?? false,
                    endDate = hasEndOverride ? o!.EndDate : null,
                    endOffsetDays = hasEndOverride ? o!.EndOffsetDays : null,
                    resolvedEndDate = resolvedEnd,
                };
            }),
        };
    }
}
