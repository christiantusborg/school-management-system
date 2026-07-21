using System.Text;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Generates the downloadable import help file (.txt): what every CSV column
/// means, formats, and the CURRENT system values (modes of study, programme
/// and specialization codes, position functions, industries, currencies,
/// nationality codes) pulled live from the database so the file never goes
/// stale.
/// </summary>
internal static class ImportHelpText
{
    internal static async Task<string> BuildAsync(
        OdinDbContext db, bool scoped, Guid? partnerId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MGW STUDENT IMPORT — HELP FILE");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine(new string('=', 70));
        sb.AppendLine();
        sb.AppendLine("HOW THE STUDENT IMPORT WORKS");
        sb.AppendLine(new string('-', 70));
        sb.AppendLine("1. Download the sample CSV and fill one row per student enrolment.");
        sb.AppendLine("2. Upload with 'Validate' first — it is a dry run, nothing is written.");
        sb.AppendLine("3. Fix any reported errors, then run 'Import'.");
        sb.AppendLine("Matching: a row with a StudentNumber that already exists updates that");
        sb.AppendLine("student; otherwise the Email is used to find an existing student; if");
        sb.AppendLine("neither matches, a NEW student is created with a login account (no");
        sb.AppendLine("email is sent). If StudentNumber is filled for a new student it is");
        sb.AppendLine("kept as an externally assigned (legacy) number.");
        sb.AppendLine("Enrolment status depends on the partner's import setting: 'direct");
        sb.AppendLine("admission' lands students at Awaiting Grades Submit; otherwise they");
        sb.AppendLine("enter the normal admission review queue.");
        sb.AppendLine();
        sb.AppendLine("STUDENT IMPORT COLUMNS");
        sb.AppendLine(new string('-', 70));
        void Col(string name, string req, string text)
        {
            sb.AppendLine($"{name} ({req})");
            sb.AppendLine($"    {text}");
        }
        Col("StudentNumber", "optional", "Existing MGW number to update that student, or your own legacy number for a new student. Leave blank to auto-generate.");
        if (!scoped)
            Col("PartnerNumber", "required", "The partner's number (shown on the partner profile). Not present in partner-scoped files — there the partner is implied.");
        Col("FirstName", "required for new students", "Given name.");
        Col("LastName", "required for new students", "Family name.");
        Col("Email", "required for new students", "Unique login email. Also used to match existing students when StudentNumber is blank.");
        Col("ProgrammeCode", "required", "The programme's CODE — see SYSTEM VALUES below.");
        Col("SpecializationCode", "required", "The specialization's CODE within that programme — see SYSTEM VALUES below.");
        Col("ModeOfStudy", "optional", "Mode id OR exact name — see SYSTEM VALUES below. Defaults to 1.");
        Col("CommencementDate", "optional", "Start of studies, format yyyy-MM-dd (e.g. 2026-09-01).");
        Col("DurationOfStudyMonths", "optional", "Whole number of months (approved duration).");
        Col("InstructionLanguage", "optional", "Free text, e.g. English.");
        Col("DateOfBirth", "optional", "Format yyyy-MM-dd.");
        Col("PassportId", "optional", "Passport / national ID as printed.");
        Col("NationalityCode", "optional", "ISO 3166-1 alpha-2 country code (DK, VN, …) — see SYSTEM VALUES below.");
        Col("Gender", "optional", "Free text; the signup wizard uses: Male, Female, Another gender identity, Prefer not to say.");
        Col("DisabilityDisclosure", "optional", "Yes / No / Prefer not to say.");
        Col("DisabilitySupportNeeds", "optional", "Free text describing support needs.");
        Col("AddressLine1 / AddressLine2 / City / StateRegion / PostalCode", "optional", "Postal address parts.");
        Col("CountryCode", "optional", "Address country as ISO 3166-1 alpha-2 code.");
        Col("Phone", "optional", "Phone number incl. country code, e.g. +45 12345678.");
        Col("HighestDegree", "optional", "Free text, e.g. Bachelor.");
        Col("DegreeSpecialization", "optional", "Free text — what the previous degree was in.");
        Col("YearsWorkExperience", "optional", "Whole number.");
        Col("PositionFunction", "optional", "Exact name from the configured list — see SYSTEM VALUES below.");
        Col("EmploymentIndustry", "optional", "Exact name from the configured list — see SYSTEM VALUES below.");
        Col("MonthlySalaryAmount", "optional", "Number, no thousand separators.");
        Col("MonthlySalaryCurrency", "optional", "Currency code — see SYSTEM VALUES below.");
        Col("WantsStudentCard", "optional", "true / false — whether a digital student ID card should be issued.");
        sb.AppendLine();
        sb.AppendLine("GRADE IMPORT COLUMNS");
        sb.AppendLine(new string('-', 70));
        Col("StudentNumber", "required", "The student's MGW number (as shown on the Students list).");
        Col("ModuleCode", "required", "The module (subject) code, e.g. BBA-101 — module codes are listed per programme on the Academic page.");
        Col("Grade", "required", "Whole number 0–100. Saved as a DRAFT into the grade sheet; the normal submit/approve flow still applies.");
        sb.AppendLine();
        sb.AppendLine("SYSTEM VALUES (live from the system at generation time)");
        sb.AppendLine(new string('=', 70));

        var modes = await db.ModesOfStudy.OrderBy(m => m.ModeOfStudyId)
            .Select(m => new { m.ModeOfStudyId, m.Name }).ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine("MODES OF STUDY (use the id or the exact name)");
        foreach (var m in modes) sb.AppendLine($"    {m.ModeOfStudyId} = {m.Name}");

        // Programmes + specialization codes; partner-scoped files list only
        // what that partner can enrol into.
        List<Guid>? allowedProgrammeIds = null;
        if (partnerId is not null)
        {
            var granted = await db.ProgrammePartners
                .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
                .Select(pp => pp.ProgrammeId).ToListAsync(ct);
            var owned = await db.Programmes
                .Where(p => p.OwnerId == partnerId && p.DeletedAt == null)
                .Select(p => p.ProgrammeId).ToListAsync(ct);
            allowedProgrammeIds = granted.Concat(owned).Distinct().ToList();
        }
        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null)
            .Where(p => allowedProgrammeIds == null || allowedProgrammeIds.Contains(p.ProgrammeId))
            .OrderBy(p => p.Code)
            .Select(p => new { p.ProgrammeId, p.Code, p.Name })
            .ToListAsync(ct);
        var programmeIds = programmes.Select(p => p.ProgrammeId).ToList();
        var specs = await db.Specializations
            .Where(s => s.DeletedAt == null && programmeIds.Contains(s.ProgrammeId))
            .OrderBy(s => s.Code)
            .Select(s => new { s.ProgrammeId, s.Code, s.Name })
            .ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine(partnerId is null
            ? "PROGRAMME CODES → SPECIALIZATION CODES (all programmes)"
            : "PROGRAMME CODES → SPECIALIZATION CODES (this partner's programmes)");
        foreach (var p in programmes)
        {
            sb.AppendLine($"    {p.Code} — {p.Name}");
            foreach (var s in specs.Where(s => s.ProgrammeId == p.ProgrammeId))
                sb.AppendLine($"        {s.Code} — {s.Name}");
        }

        var positions = await db.PositionFunctions.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine("POSITION FUNCTIONS (exact names)");
        foreach (var p in positions) sb.AppendLine($"    {p}");

        var industries = await db.EmploymentIndustries.OrderBy(i => i.Name).Select(i => i.Name).ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine("EMPLOYMENT INDUSTRIES (exact names)");
        foreach (var i in industries) sb.AppendLine($"    {i}");

        var currencies = await db.Currencies.OrderBy(c => c.Code).Select(c => c.Code).ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine("CURRENCY CODES");
        sb.AppendLine("    " + string.Join(", ", currencies));

        var nationalities = await db.Nationalities.OrderBy(n => n.Code)
            .Select(n => new { n.Code, n.Name }).ToListAsync(ct);
        sb.AppendLine();
        sb.AppendLine("NATIONALITY / COUNTRY CODES (ISO 3166-1 alpha-2)");
        foreach (var n in nationalities) sb.AppendLine($"    {n.Code} — {n.Name}");

        return sb.ToString();
    }
}
