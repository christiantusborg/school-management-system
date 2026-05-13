using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Letters;

public sealed record TranscriptGradeRow(string Code, string Name, int Ects, decimal Score);

public sealed class LetterTagResolver(OdinDbContext db)
{
    /// <summary>
    /// Raw subject grade rows for an enrollment, used by the certificate
    /// renderer when expanding a "transcriptTable" field. Returns rows in the
    /// order they were graded (or by subject code as a stable fallback).
    /// </summary>
    public async Task<IReadOnlyList<TranscriptGradeRow>> ResolveTranscriptRowsAsync(
        Guid enrollmentId, CancellationToken ct)
    {
        var rows = await db.SubjectGrades
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .Select(g => new
            {
                g.Score,
                g.GradedAt,
                Subject = db.Subjects
                    .Where(s => s.SubjectId == g.SubjectId)
                    .Select(s => new { s.Code, s.Name, s.Ects })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        return rows
            .OrderBy(r => r.GradedAt ?? DateTime.MinValue)
            .ThenBy(r => r.Subject?.Code ?? string.Empty)
            .Select(r => new TranscriptGradeRow(
                r.Subject?.Code ?? string.Empty,
                r.Subject?.Name ?? string.Empty,
                r.Subject?.Ects ?? 0,
                r.Score))
            .ToList();
    }

    /// <summary>
    /// Resolves every tag in <see cref="LetterTagRegistry"/> for the supplied
    /// enrollment. Missing data resolves to empty string rather than throwing
    /// so a partially-complete student still produces a renderable letter.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.PartnerId,
                e.SpecializationId,
                e.CommencementDate,
                e.ModeOfStudyId,
                Student = db.Students
                    .Where(s => s.StudentId == e.StudentId)
                    .Select(s => new
                    {
                        s.StudentNumber,
                        s.PassportId,
                        s.DateOfBirth,
                        UserId = s.UserId,
                    })
                    .FirstOrDefault(),
                Partner = db.Partners
                    .Where(p => p.PartnerId == e.PartnerId)
                    .Select(p => new { p.Name })
                    .FirstOrDefault(),
                Specialization = db.Specializations
                    .Where(sp => sp.SpecializationId == e.SpecializationId)
                    .Select(sp => new
                    {
                        sp.Name,
                        sp.ProgrammeId,
                        sp.DurationOfStudyMonths,
                        sp.InstructionLanguage,
                    })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in LetterTagRegistry.All)
            result[tag.Token] = string.Empty;

        if (enrollment is null) return result;

        // ── Student profile ──────────────────────────────────────────────
        string firstName = string.Empty;
        string surname = string.Empty;
        if (enrollment.Student?.UserId is { } userId)
        {
            var profile = await db.UserProfiles
                .Where(p => p.UserId == userId)
                .Select(p => new { p.FirstName, p.LastName })
                .FirstOrDefaultAsync(ct);
            firstName = profile?.FirstName ?? string.Empty;
            surname = profile?.LastName ?? string.Empty;
        }
        var fullName = string.Join(' ', new[] { firstName, surname }.Where(s => !string.IsNullOrWhiteSpace(s)));

        result["[student full name]"] = fullName;
        result["[student firstname]"] = firstName;
        result["[student surname]"]   = surname;
        result["[student number]"]    = enrollment.Student?.StudentNumber ?? string.Empty;
        result["[student address]"]   = string.Empty; // No address on Student today; fill in a follow-up.
        result["[passport id]"]       = enrollment.Student?.PassportId ?? string.Empty;
        result["[date]"]              = DateTime.UtcNow.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        result["[commencement date]"] = enrollment.CommencementDate?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        result["[duration of study]"] = enrollment.Specialization is null
            ? string.Empty
            : $"{enrollment.Specialization.DurationOfStudyMonths} months";
        result["[instruction language]"] = enrollment.Specialization?.InstructionLanguage ?? string.Empty;
        var calculatedCompletion = (enrollment.CommencementDate is { } start && enrollment.Specialization is { } sp)
            ? start.AddMonths(sp.DurationOfStudyMonths)
            : (DateTime?)null;
        result["[completion date]"] = calculatedCompletion?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        result["[graduation date]"] = calculatedCompletion?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        result["[date of birth]"] = enrollment.Student?.DateOfBirth?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        var modeOfStudy = await db.ModesOfStudy
            .Where(m => m.ModeOfStudyId == enrollment.ModeOfStudyId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct);
        result["[mode of study]"] = modeOfStudy ?? string.Empty;

        // ── Partner / Programme / Specialization ─────────────────────────
        result["[partner name]"]        = enrollment.Partner?.Name ?? string.Empty;
        result["[specialization name]"] = enrollment.Specialization?.Name ?? string.Empty;

        if (enrollment.Specialization is not null)
        {
            var programmeName = await db.Programmes
                .Where(p => p.ProgrammeId == enrollment.Specialization.ProgrammeId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
            result["[program name]"] = programmeName ?? string.Empty;
        }

        // ── Grades ───────────────────────────────────────────────────────
        var grades = await db.SubjectGrades
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .Select(g => new
            {
                g.Score,
                g.GradedAt,
                Subject = db.Subjects.Where(s => s.SubjectId == g.SubjectId)
                    .Select(s => new { s.Code, s.Name, s.Ects }).FirstOrDefault(),
            })
            .ToListAsync(ct);

        if (grades.Count > 0)
        {
            var avg = grades.Average(g => (double)g.Score);
            result["[grade]"] = avg.ToString("0.0", CultureInfo.InvariantCulture);
        }

        var transcriptRows = grades.Select(g => new TranscriptRow(
            g.Subject?.Code ?? string.Empty,
            g.Subject?.Name ?? string.Empty,
            g.Subject?.Ects ?? 0,
            g.Score,
            g.GradedAt)).ToList();

        result["[transcript]"] = BuildTranscriptHtml(transcriptRows);
        result["[ects achieved]"] = transcriptRows.Sum(r => r.Ects).ToString(CultureInfo.InvariantCulture);

        return result;
    }

    private sealed record TranscriptRow(string Code, string Name, int Ects, decimal Score, DateTime? GradedAt);

    /// <summary>
    /// Maps a numeric exam score (0–100) onto the IBSS grading scale described
    /// in the official Blank Transcript template (08.10.2025).
    /// </summary>
    private static (string EctsGrade, string UkGrade, double GradePoint) MapScore(decimal score)
    {
        var s = (int)Math.Floor(score);
        if (s >= 75) return ("A",  "A+", 5.0);
        if (s >= 70) return ("A",  "A",  5.0);
        if (s >= 65) return ("B",  "A-", 4.0);
        if (s >= 60) return ("C",  "B+", 3.0);
        if (s >= 55) return ("C",  "B",  3.0);
        if (s >= 50) return ("D",  "B-", 2.0);
        if (s >= 45) return ("D",  "C+", 2.0);
        if (s >= 41) return ("E",  "C",  1.0);
        if (s == 40) return ("E",  "C-", 1.0);
        if (s >= 30) return ("FX", "F",  0.0);
        return            ("F",  "F",  0.0);
    }

    private static string BuildTranscriptHtml(IReadOnlyList<TranscriptRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<table style='width:100%;border-collapse:collapse;'>");
        sb.Append("<thead><tr>")
            .Append("<th style='text-align:left;border-bottom:1px solid #999;padding:4px;'>Code</th>")
            .Append("<th style='text-align:left;border-bottom:1px solid #999;padding:4px;'>Module</th>")
            .Append("<th style='text-align:right;border-bottom:1px solid #999;padding:4px;'>ECTS credit hours</th>")
            .Append("<th style='text-align:center;border-bottom:1px solid #999;padding:4px;'>ECTS Grade</th>")
            .Append("<th style='text-align:center;border-bottom:1px solid #999;padding:4px;'>IBSS Grade</th>")
            .Append("<th style='text-align:right;border-bottom:1px solid #999;padding:4px;'>ECTS Grade Point</th>")
            .Append("<th style='text-align:right;border-bottom:1px solid #999;padding:4px;'>Grade Point</th>")
            .Append("</tr></thead><tbody>");

        if (rows.Count == 0)
        {
            sb.Append("<tr><td colspan='7' style='padding:6px;font-style:italic;color:#888;'>No grades recorded.</td></tr>");
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        var totalEcts = 0;
        var totalGradePoint = 0.0;
        foreach (var r in rows)
        {
            var (ects, uk, gp) = MapScore(r.Score);
            var rowGradePoint = r.Ects * gp;
            totalEcts += r.Ects;
            totalGradePoint += rowGradePoint;
            sb.Append("<tr>")
                .Append($"<td style='padding:4px;'>{System.Net.WebUtility.HtmlEncode(r.Code)}</td>")
                .Append($"<td style='padding:4px;'>{System.Net.WebUtility.HtmlEncode(r.Name)}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{r.Ects}</td>")
                .Append($"<td style='padding:4px;text-align:center;'>{ects}</td>")
                .Append($"<td style='padding:4px;text-align:center;'>{uk}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{gp.ToString("0.0", CultureInfo.InvariantCulture)}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{rowGradePoint.ToString("0.0", CultureInfo.InvariantCulture)}</td>")
                .Append("</tr>");
        }
        var gpa = totalEcts > 0 ? totalGradePoint / totalEcts : 0.0;
        sb.Append("<tr style='border-top:1px solid #999;'>")
            .Append("<td colspan='2' style='padding:4px;'><strong>Total</strong></td>")
            .Append($"<td style='padding:4px;text-align:right;'><strong>{totalEcts}</strong></td>")
            .Append("<td colspan='3'></td>")
            .Append($"<td style='padding:4px;text-align:right;'><strong>{totalGradePoint.ToString("0.0", CultureInfo.InvariantCulture)}</strong></td>")
            .Append("</tr>");
        sb.Append("<tr>")
            .Append("<td colspan='6' style='padding:4px;'><strong>Grade Point Average</strong></td>")
            .Append($"<td style='padding:4px;text-align:right;'><strong>{gpa.ToString("0.00", CultureInfo.InvariantCulture)}</strong></td>")
            .Append("</tr>");
        sb.Append("</tbody></table>");
        return sb.ToString();
    }
}
