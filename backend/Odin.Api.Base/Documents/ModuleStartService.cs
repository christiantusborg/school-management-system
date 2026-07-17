using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Documents;

/// <summary>
/// Resolves the per-module start dates of an enrolment: every module of the
/// enrolment's CURRENT specialization with its override (explicit date or
/// commencement + N days) and the resolved date. Default (no override row)
/// is the enrolment's commencement date. Shared by the admin (read/write),
/// partner (read) and student (read) endpoints.
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
            .Select(s => new { s.SubjectId, s.Code, s.Name })
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
                DateTime? resolved = o is null
                    ? commencement
                    : o.UseOffset
                        ? commencement?.AddDays(o.OffsetDays ?? 0)
                        : o.StartDate;
                return new
                {
                    subjectId = s.SubjectId,
                    code = s.Code,
                    name = s.Name,
                    hasOverride = o is not null,
                    useOffset = o?.UseOffset ?? false,
                    startDate = o?.StartDate,
                    offsetDays = o?.OffsetDays,
                    resolvedDate = resolved,
                };
            }),
        };
    }
}
