using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Issues the Digital Student Card at offer acceptance when the enrolment's
/// programme has IssueDigitalStudentCard on. Best-effort by design: a
/// template or render problem must never block the acceptance flow — the
/// Admission Office can always generate the card later from the Letters tab.
/// Shared by all three acceptance paths (student, partner on behalf,
/// Admission on behalf).
/// </summary>
public static class StudentCardIssuer
{
    public static async Task TryIssueAsync(
        OdinDbContext db, LetterReleaseService letterRelease,
        Guid specializationId, Guid enrollmentId, CancellationToken ct)
    {
        try
        {
            var issueCard = await db.Specializations
                .Where(s => s.SpecializationId == specializationId)
                .Select(s => s.Programmes.IssueDigitalStudentCard)
                .FirstOrDefaultAsync(ct);
            if (!issueCard) return;
            // Respect the student's wizard opt-out ("Yes, I would like a
            // digital student card" left unchecked).
            var wantsCard = await db.Enrollments
                .Where(e => e.StudentEnrollmentId == enrollmentId)
                .Select(e => db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.WantsStudentIdCard).FirstOrDefault())
                .FirstOrDefaultAsync(ct);
            if (!wantsCard) return;
            await letterRelease.ReleaseAsync(enrollmentId, LetterType.StudentIdCard, ct);
        }
        catch
        {
            // Card generation is a bonus of acceptance, not a condition of it.
        }
    }
}
