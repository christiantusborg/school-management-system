using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

public sealed record TranscriptGradeRow(string Code, string Name, decimal Ects, decimal Score);

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
        // Grades follow the enrolment's CURRENT specialization: when the
        // Admission Office moves an enrolment to another programme, scores
        // recorded for the previous specialization's subjects must not leak
        // into the new programme's transcript/certificates.
        var currentSpecId = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.SpecializationId)
            .FirstOrDefaultAsync(ct);

        var rows = await db.SubjectGrades
            .Where(g => g.StudentEnrollmentId == enrollmentId
                && db.Subjects.Any(s => s.SubjectId == g.SubjectId && s.SpecializationId == currentSpecId))
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
        Guid enrollmentId, CancellationToken ct, string? reference = null,
        LetterType? letterType = null)
    {
        var enrollment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.PartnerId,
                e.SpecializationId,
                e.InstructionLanguageOverride,
                e.OfferLetterDate,
                e.AdmissionLetterDate,
                e.TranscriptDate,
                e.GraduationDate,
                e.ProjectTitle,
                e.CommencementDate,
                e.ModeOfStudyId,
                e.ApprovedDurationValue,
                e.ApprovedDurationUnit,
                Student = db.Students
                    .Where(s => s.StudentId == e.StudentId)
                    .Select(s => new
                    {
                        s.StudentNumber,
                        s.StudentCardId,
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

        // The reference code is composed (with its letter-type prefix) and
        // persisted by LetterReleaseService, then passed in here — the
        // resolver only echoes it into the [ref] tag.
        result["[ref]"] = reference ?? string.Empty;

        if (enrollment is null) return result;

        // ── Student profile ──────────────────────────────────────────────
        string firstName = string.Empty;
        string surname = string.Empty;
        string address = string.Empty;
        if (enrollment.Student?.UserId is { } userId)
        {
            var profile = await db.UserProfiles
                .Where(p => p.UserId == userId)
                .Select(p => new { p.FirstName, p.LastName })
                .FirstOrDefaultAsync(ct);
            firstName = profile?.FirstName ?? string.Empty;
            surname = profile?.LastName ?? string.Empty;

            // Address lives on UserAddresses (not the Student). Prefer the
            // primary row; fall back to any row so a student who never flagged
            // one still gets an address. Compose the non-blank parts so a
            // partial address never renders stray commas.
            var addr = await db.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsPrimary)
                .Select(a => new { a.Street, a.City, a.State, a.ZipCode, a.Country })
                .FirstOrDefaultAsync(ct);
            if (addr is not null)
                address = string.Join(", ", new[] { addr.Street, addr.City, addr.State, addr.ZipCode, addr.Country }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));
        }
        var fullName = string.Join(' ', new[] { firstName, surname }.Where(s => !string.IsNullOrWhiteSpace(s)));

        result["[student full name]"] = fullName;
        result["[student firstname]"] = firstName;
        result["[student surname]"]   = surname;
        // The Student ID Card may carry a per-student card ID that overrides
        // the real student number ON THE CARD only; every other letter keeps
        // the real number.
        var isIdCard = reference?.Contains("-IDCARD-", StringComparison.OrdinalIgnoreCase) == true;
        result["[student number]"]    = (isIdCard && !string.IsNullOrWhiteSpace(enrollment.Student?.StudentCardId)
            ? enrollment.Student!.StudentCardId
            : enrollment.Student?.StudentNumber) ?? string.Empty;
        result["[student address]"]   = address;
        result["[passport id]"]       = enrollment.Student?.PassportId ?? string.Empty;

        // ── Final-project details from assignment uploads ────────────────
        // Project name / Supervisor / Word count live on the PER-STUDENT
        // assignment uploads (Uploaded Assignments tab), filled by partner
        // staff, teachers or the Admission Office on uploads in the
        // dissertation cohorts. The letter being rendered picks which cohort's
        // module to read first: the proposal approval letter prefers the
        // "Dissertation Proposal" cohort's uploads, everything else the
        // final-project cohort's. The other cohort's uploads fill gaps, and
        // within a cohort the newest value wins.
        var dissertationUploads = await db.ModuleCohortStudents
            .Where(cs => cs.StudentEnrollmentId == enrollmentId && cs.DeletedAt == null)
            .Join(db.ModuleCohorts.Where(c => c.DeletedAt == null),
                cs => cs.ModuleCohortId, c => c.ModuleCohortId, (cs, c) => c)
            .Select(c => new
            {
                c.SubjectId,
                TypeName = db.CohortTypes
                    .Where(t => t.CohortTypeId == c.CohortTypeId)
                    .Select(t => t.Name)
                    .FirstOrDefault() ?? string.Empty,
                Uploads = db.AssignmentUploads
                    .Where(a => a.StudentEnrollmentId == enrollmentId
                        && a.SubjectId == c.SubjectId && a.DeletedAt == null)
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new { a.ProjectName, a.SupervisorName, a.WordCount })
                    .ToList(),
            })
            .ToListAsync(ct);

        // "Dissertation Proposal" also contains "dissertation", so proposal
        // is checked first — same heuristic as the grade-save auto-release.
        static bool IsProposalType(string n) => n.Contains("proposal", StringComparison.OrdinalIgnoreCase);
        static bool IsFinalType(string n) =>
            n.Contains("final project", StringComparison.OrdinalIgnoreCase)
            || n.Contains("dissertation", StringComparison.OrdinalIgnoreCase);
        var preferProposal = letterType == LetterType.FinalProposalApproval;
        var orderedUploads = dissertationUploads
            .Where(c => IsProposalType(c.TypeName) || IsFinalType(c.TypeName))
            .OrderByDescending(c => IsProposalType(c.TypeName) == preferProposal)
            .SelectMany(c => c.Uploads)
            .ToList();
        string FirstFilled(IEnumerable<string?> values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

        result["[supervisor]"] = FirstFilled(orderedUploads.Select(u => u.SupervisorName));
        result["[word count]"] = FirstFilled(orderedUploads.Select(u => u.WordCount?.ToString(CultureInfo.InvariantCulture)));
        // The upload's Project name wins over the enrolment's Project Title
        // from the grade-submit modal when both are filled.
        var uploadProjectName = FirstFilled(orderedUploads.Select(u => u.ProjectName));
        result["[project title]"]     = !string.IsNullOrWhiteSpace(uploadProjectName)
            ? uploadProjectName
            : enrollment.ProjectTitle ?? string.Empty;
        // [date] is the letter's issuance date. Offer/admission letters honour
        // an Admission-Office override (reference prefix tells which letter this
        // is: IBSS-OL-… = offer, IBSS-AL-… = admission); everything else uses
        // the render date.
        var issuanceDate = DateTime.UtcNow;
        if (reference is not null)
        {
            if (reference.Contains("-OL-", StringComparison.OrdinalIgnoreCase) && enrollment.OfferLetterDate is { } od)
                issuanceDate = od;
            else if (reference.Contains("-AL-", StringComparison.OrdinalIgnoreCase) && enrollment.AdmissionLetterDate is { } ad)
                issuanceDate = ad;
            else if (reference.Contains("-PTR-", StringComparison.OrdinalIgnoreCase) && enrollment.TranscriptDate is { } ptd)
                issuanceDate = ptd;
            else if (reference.Contains("-TR-", StringComparison.OrdinalIgnoreCase) && enrollment.TranscriptDate is { } td)
                issuanceDate = td;
        }
        result["[date]"]              = issuanceDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        result["[commencement date]"] = enrollment.CommencementDate?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        // Approved (per-enrolment) duration override — stored as value+unit
        // ("Month"/"Day") — wins over the specialization's month default and
        // shifts completion too. Prints exactly what was entered: months as
        // "N months", days as "N days".
        var overrideValue = enrollment.ApprovedDurationValue;
        var overrideUnit = enrollment.ApprovedDurationUnit;
        var specMonths = enrollment.Specialization?.DurationOfStudyMonths;
        result["[duration of study]"] = overrideValue is not null
            ? SharedLibrary.Basics.Opaque.Domains.DurationDays.Display(overrideValue, overrideUnit)
            : specMonths is { } dm ? $"{dm} months" : string.Empty;
        // Per-enrolment override wins over the specialization's language.
        result["[instruction language]"] = !string.IsNullOrWhiteSpace(enrollment.InstructionLanguageOverride)
            ? enrollment.InstructionLanguageOverride!
            : enrollment.Specialization?.InstructionLanguage ?? string.Empty;
        // The study period is inclusive of its first day, so a 12-month course
        // that commences 18 Jun 2026 runs through 17 Jun 2027 — the day before
        // the anniversary. Subtract one day so the printed completion (and
        // graduation) date is the last day of study, not the first day after.
        var calculatedCompletion = SharedLibrary.Basics.Opaque.Domains.DurationDays.ExpectedCompletion(
            enrollment.CommencementDate, overrideValue, overrideUnit, specMonths);
        result["[completion date]"] = calculatedCompletion?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        // Graduation defaults to the expected completion date; an Admission-Office
        // override on the enrolment wins when set.
        var graduation = enrollment.GraduationDate ?? calculatedCompletion;
        result["[graduation date]"] = graduation?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        result["[date of birth]"] = enrollment.Student?.DateOfBirth?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
        result["[student phone]"] = enrollment.Student?.UserId is { } phoneUid
            ? await db.UserPhones.Where(p => p.UserId == phoneUid && p.IsPrimary)
                .Select(p => p.Number).FirstOrDefaultAsync(ct) ?? string.Empty
            : string.Empty;
        var modeOfStudy = await db.ModesOfStudy
            .Where(m => m.ModeOfStudyId == enrollment.ModeOfStudyId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct);
        result["[mode of study]"] = modeOfStudy ?? string.Empty;

        // ── Partner / Programme / Specialization ─────────────────────────
        result["[partner name]"]        = enrollment.Partner?.Name ?? string.Empty;
        result["[specialization name]"] = enrollment.Specialization?.Name ?? string.Empty;

        // [valid until] = end date of the partner's most-recent contract
        // (primarily used on partner certificates; resolved here too so the
        // tag never renders literally on a student letter).
        var partnerContractEnd = await db.PartnerContracts
            .Where(c => c.PartnerId == enrollment.PartnerId && c.DeletedAt == null)
            .OrderByDescending(c => c.StartDate)
            .Select(c => c.EndDate)
            .FirstOrDefaultAsync(ct);
        result["[valid until]"] = partnerContractEnd?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

        if (enrollment.Specialization is not null)
        {
            var prog = await db.Programmes
                .Where(p => p.ProgrammeId == enrollment.Specialization.ProgrammeId)
                .Select(p => new { p.Name, SchoolName = p.School != null ? p.School.Name : null })
                .FirstOrDefaultAsync(ct);
            result["[program name]"] = prog?.Name ?? string.Empty;
            result["[school name]"] = prog?.SchoolName ?? string.Empty;
        }

        // ── Grades ───────────────────────────────────────────────────────
        // Same current-specialization filter as ResolveTranscriptRowsAsync.
        var grades = await db.SubjectGrades
            .Where(g => g.StudentEnrollmentId == enrollmentId
                && db.Subjects.Any(s => s.SubjectId == g.SubjectId
                    && s.SpecializationId == enrollment.SpecializationId))
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
        result["[ects achieved]"] = transcriptRows.Sum(r => r.Ects).ToString("0.##", CultureInfo.InvariantCulture);

        // ── Fees / payment plan (offer + admission letter fee section) ────
        // "-" (not blank) when no payment plan exists yet, so a letter's
        // "Tuition Fee:" line never trails into nothing.
        result["[tuition fee]"] = "-";
        result["[additional fees]"] = "-";
        result["[total fees]"] = "-";
        result["[number of payments]"] = "-";
        result["[payment plan]"] = string.Empty;

        var plan = await db.EnrollmentPaymentPlans
            .Where(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null)
            .Select(p => new
            {
                p.TotalTuitionFee,
                p.Currency,
                Installments = p.Installments.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.Amount, i.DueDate, i.PayByCardEnabled, i.PayByBankEnabled })
                    .ToList(),
                AdditionalInvoices = p.AdditionalInvoices.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.DueDate, i.PayByCardEnabled, i.PayByBankEnabled, i.LinesJson })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (plan is not null)
        {
            string Money(decimal v) => $"{plan.Currency} {v.ToString("N2", CultureInfo.InvariantCulture)}";
            static string MethodOf(bool bank, bool card) => (bank, card) switch
            {
                (true, true)  => "Bank transfer / Credit card",
                (true, false) => "Bank transfer",
                (false, true) => "Credit card",
                _             => "As advised",
            };
            var addInvoices = plan.AdditionalInvoices.Select(a => new
            {
                a.Sequence, a.DueDate, a.PayByBankEnabled, a.PayByCardEnabled,
                Total = Payments.AdditionalInvoiceLines.Total(a.LinesJson),
                Title = Payments.AdditionalInvoiceLines.Title(a.LinesJson, a.Sequence),
            }).ToList();
            var additionalTotal = addInvoices.Sum(a => a.Total);

            result["[tuition fee]"] = Money(plan.TotalTuitionFee);
            if (addInvoices.Count > 0)
                result["[additional fees]"] = string.Join("\n", addInvoices.Select(a => $"{a.Title}: {Money(a.Total)}"));
            result["[total fees]"] = Money(plan.TotalTuitionFee + additionalTotal);
            result["[number of payments]"] = plan.Installments.Count.ToString(CultureInfo.InvariantCulture);
            // One line per installment (then per additional invoice): method,
            // amount, due date. Plain text so it renders inside a positioned
            // letter text field.
            result["[payment plan]"] = string.Join("\n", plan.Installments.Select(i =>
            {
                var due = i.DueDate?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? "TBC";
                return $"Payment {i.Sequence}: {MethodOf(i.PayByBankEnabled, i.PayByCardEnabled)}, {Money(i.Amount)}, due {due}";
            }).Concat(addInvoices.Select(a =>
            {
                var due = a.DueDate?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? "TBC";
                return $"{a.Title}: {MethodOf(a.PayByBankEnabled, a.PayByCardEnabled)}, {Money(a.Total)}, due {due} (one-time payment)";
            })));
        }

        return result;
    }

    private sealed record TranscriptRow(string Code, string Name, decimal Ects, decimal Score, DateTime? GradedAt);

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
            .Append("<th style='text-align:center;border-bottom:1px solid #999;padding:4px;'>School Grade</th>")
            .Append("<th style='text-align:right;border-bottom:1px solid #999;padding:4px;'>ECTS Grade Point</th>")
            .Append("<th style='text-align:right;border-bottom:1px solid #999;padding:4px;'>Grade Point</th>")
            .Append("</tr></thead><tbody>");

        if (rows.Count == 0)
        {
            sb.Append("<tr><td colspan='7' style='padding:6px;font-style:italic;color:#888;'>No grades recorded.</td></tr>");
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        var totalEcts = 0m;
        var totalGradePoint = 0.0;
        foreach (var r in rows)
        {
            var (ects, uk, gp) = MapScore(r.Score);
            var rowGradePoint = (double)r.Ects * gp;
            totalEcts += r.Ects;
            totalGradePoint += rowGradePoint;
            sb.Append("<tr>")
                .Append($"<td style='padding:4px;'>{System.Net.WebUtility.HtmlEncode(r.Code)}</td>")
                .Append($"<td style='padding:4px;'>{System.Net.WebUtility.HtmlEncode(r.Name)}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{r.Ects.ToString("0.##", CultureInfo.InvariantCulture)}</td>")
                .Append($"<td style='padding:4px;text-align:center;'>{ects}</td>")
                .Append($"<td style='padding:4px;text-align:center;'>{uk}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{gp.ToString("0.0", CultureInfo.InvariantCulture)}</td>")
                .Append($"<td style='padding:4px;text-align:right;'>{rowGradePoint.ToString("0.0", CultureInfo.InvariantCulture)}</td>")
                .Append("</tr>");
        }
        var gpa = totalEcts > 0 ? totalGradePoint / (double)totalEcts : 0.0;
        sb.Append("<tr style='border-top:1px solid #999;'>")
            .Append("<td colspan='2' style='padding:4px;'><strong>Total</strong></td>")
            .Append($"<td style='padding:4px;text-align:right;'><strong>{totalEcts.ToString("0.##", CultureInfo.InvariantCulture)}</strong></td>")
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
