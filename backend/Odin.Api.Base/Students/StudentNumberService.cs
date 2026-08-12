using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Students;

/// <summary>
/// Mints student numbers in the <c>ST-&lt;commencement date&gt;-&lt;running
/// number&gt;</c> scheme, e.g. <c>ST-20240201-001</c>. The running number is
/// scoped to the calendar MONTH of the commencement date and shared across the
/// whole system, so two students commencing anywhere in Feb 2024 get -001 and
/// -002 regardless of the exact day.
///
/// A student created before any commencement date exists (draft/signup) gets a
/// temporary <c>ST-TMP-xxxxxx</c> number; <see cref="FinaliseIfTempAsync"/>
/// rewrites it to the real number the moment the student's first enrolment
/// gets a commencement date. Existing students (real, non-temp numbers) are
/// never renumbered — the new scheme applies to new students only.
/// </summary>
public static class StudentNumberService
{
    private const string TempPrefix = "ST-TMP-";

    /// <summary>Placeholder number for a student with no commencement date yet.</summary>
    public static string Temp()
    {
        var rnd = Guid.NewGuid().ToString("N").ToUpperInvariant()[..6];
        return $"{TempPrefix}{rnd}";
    }

    public static bool IsTemp(string? number) =>
        number is not null && number.StartsWith(TempPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Mint <c>ST-YYYYMMDD-NNN</c> for the given commencement date. NNN is the
    /// next running number within that commencement month across the whole
    /// system. Counts both persisted students and un-saved ones already tracked
    /// in this <see cref="OdinDbContext"/> (so a bulk import numbers its rows
    /// sequentially before the transaction commits).
    /// </summary>
    public static async Task<string> MintAsync(OdinDbContext db, DateTime commencement, CancellationToken ct)
    {
        var datePart = commencement.ToString("yyyyMMdd");
        var monthPrefix = $"ST-{commencement:yyyyMM}";   // matches every day in that month

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var persisted = await db.Students
                .Where(s => s.StudentNumber != null && s.StudentNumber.StartsWith(monthPrefix))
                .Select(s => s.StudentNumber!)
                .ToListAsync(ct);
            var pending = db.Students.Local
                .Where(s => s.StudentNumber != null && s.StudentNumber.StartsWith(monthPrefix, StringComparison.Ordinal))
                .Select(s => s.StudentNumber!);

            var next = NextCounter(persisted.Concat(pending)) + attempt;
            var candidate = $"ST-{datePart}-{next:000}";
            if (!await db.Students.AnyAsync(s => s.StudentNumber == candidate, ct)
                && db.Students.Local.All(s => s.StudentNumber != candidate))
                return candidate;
        }

        // Extremely unlikely: fall back to a unique random suffix on the date.
        var tail = Guid.NewGuid().ToString("N").ToUpperInvariant()[..4];
        return $"ST-{datePart}-{tail}";
    }

    private static int NextCounter(IEnumerable<string> monthNumbers)
    {
        var max = 0;
        foreach (var n in monthNumbers)
        {
            var dash = n.LastIndexOf('-');
            if (dash < 0) continue;
            if (int.TryParse(n[(dash + 1)..], out var v) && v > max) max = v;
        }
        return max + 1;
    }

    /// <summary>
    /// If the student still carries a temporary number, replace it with the
    /// final <c>ST-&lt;commencement&gt;-&lt;monthly no&gt;</c> derived from the
    /// earliest commencement date among their enrolments. No-op for real/legacy
    /// numbers or when no enrolment has a commencement date yet. The caller is
    /// responsible for saving (this only mutates the tracked entity).
    /// </summary>
    public static async Task FinaliseIfTempAsync(OdinDbContext db, Guid studentId, CancellationToken ct)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null || !IsTemp(student.StudentNumber)) return;

        var commencement = await db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null && e.CommencementDate != null)
            .OrderBy(e => e.CommencementDate)
            .Select(e => e.CommencementDate)
            .FirstOrDefaultAsync(ct);
        if (commencement is not { } c) return;

        student.StudentNumber = await MintAsync(db, c, ct);
    }
}
