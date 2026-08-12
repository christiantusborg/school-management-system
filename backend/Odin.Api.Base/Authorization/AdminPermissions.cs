namespace Odin.Api.Base.Authorization;

/// <summary>
/// The Phase-1 permission catalogue for the configurable access matrix.
///
/// A permission is a named, gated action (e.g. <c>students.delete</c>). The
/// catalogue itself is code — it's defined by the app's features. WHICH role
/// holds each permission is data (the <c>RolePermission</c> table); this class
/// only carries the <b>seed</b> defaults that reproduce the access rules that
/// were previously hard-coded across ~30 endpoints, so nothing changes on the
/// day the matrix ships.
///
/// <see cref="AdminLevels.SuperAdministrator"/> is NOT listed in any
/// <see cref="PermissionDef.DefaultRoles"/>: SuperAdmin bypasses the matrix and
/// always has every permission (enforced in the permission service), so there
/// is no seeded Super grant to store.
/// </summary>
public static class AdminPermissions
{
    // ── Keys ──────────────────────────────────────────────────────────────
    // Security & roles
    public const string AdminUsersManage = "admin_users.manage";
    public const string RolesManage      = "roles.manage";
    public const string ChangelogView    = "changelog.view";

    // Students & enrolment
    public const string StudentsView             = "students.view";
    public const string StudentsSubmitGrade      = "students.submit_grade";
    public const string StudentsSetHandledBy     = "students.set_handled_by";
    public const string StudentsEditSpecialization = "students.edit_specialization";
    public const string StudentsEditDuration     = "students.edit_duration";
    public const string StudentsEditCommencement = "students.edit_commencement";
    public const string StudentsEditLetterDates  = "students.edit_letter_dates";
    public const string StudentsEditLegacyId     = "students.edit_legacy_id";
    public const string StudentsSendLetterEmail  = "students.send_letter_email";
    public const string StudentsRegenerateLetters = "students.regenerate_letters";
    public const string StudentsDelete           = "students.delete";
    public const string StudentsRemoveProgramme  = "students.remove_programme";
    public const string StudentsExport           = "students.export";

    // Letters
    public const string LetterTypesManage = "letter_types.manage";
    public const string LettersGenerate   = "letters.generate";
    public const string LettersEmailEdit  = "letters.email_edit";

    // CRM / Mail / Statistics
    public const string CrmView            = "crm.view";
    public const string CrmSettings        = "crm.settings";
    public const string MailAccountsManage = "mail.accounts_manage";
    public const string MailSettings       = "mail.settings";
    public const string StatisticsView     = "statistics.view";
    public const string StatisticsFinancial = "statistics.financial";

    // Partners / Programmes / Invoices
    public const string PartnersAssignSales   = "partners.assign_sales";
    public const string ProgrammesEditDuration = "programmes.edit_duration";
    public const string InvoicesMarkPaid      = "invoices.mark_paid";
    public const string InvoicesDelete        = "invoices.delete";

    public sealed record PermissionDef(string Key, string Area, string Label, string Description, string[] DefaultRoles);

    // Convenience default-role groups (SuperAdmin is never listed — it bypasses).
    private static readonly string[] AllLevels = [AdminLevels.Administrator, AdminLevels.Manager, AdminLevels.Editor, AdminLevels.Viewer, AdminLevels.Sales];
    private static readonly string[] AdminOnly = [AdminLevels.Administrator];
    private static readonly string[] EditorPlus = [AdminLevels.Administrator, AdminLevels.Manager, AdminLevels.Editor];
    private static readonly string[] ExceptSales = [AdminLevels.Administrator, AdminLevels.Manager, AdminLevels.Editor, AdminLevels.Viewer];
    private static readonly string[] SuperOnly = [];

    /// <summary>The full catalogue, in display order (grouped by area).</summary>
    public static readonly IReadOnlyList<PermissionDef> Catalog =
    [
        // Security & roles
        new(AdminUsersManage, "Security & roles", "Manage admin users", "Create, edit and delete admin users; reset passwords.", SuperOnly),
        new(RolesManage,      "Security & roles", "Manage roles & permissions", "Edit this access matrix.", SuperOnly),
        new(ChangelogView,    "Security & roles", "View changelog", "See the in-app changelog tab.", SuperOnly),

        // Students & enrolment
        new(StudentsView,             "Students", "View students", "Open the students area (Sales scoped to own partners).", AllLevels),
        new(StudentsSubmitGrade,      "Students", "Submit grades", "Submit or edit a student's grade.", EditorPlus),
        new(StudentsSetHandledBy,     "Students", "Set \"Handled by\"", "Set the Sales attribution on a student.", EditorPlus),
        new(StudentsEditSpecialization, "Students", "Edit specialization", "Change a student's specialization.", AdminOnly),
        new(StudentsEditDuration,     "Students", "Edit enrolment duration", "Edit the approved enrolment duration.", AdminOnly),
        new(StudentsEditCommencement, "Students", "Edit commencement date", "Edit the enrolment commencement date.", AdminOnly),
        new(StudentsEditLetterDates,  "Students", "Edit letter dates", "Edit the dates on issued letters.", AdminOnly),
        new(StudentsEditLegacyId,     "Students", "Edit legacy student ID", "Override the manual/legacy Student ID and old-student flag.", AdminOnly),
        new(StudentsSendLetterEmail,  "Students", "Send letter email", "Email an issued letter to the student.", AdminOnly),
        new(StudentsRegenerateLetters, "Students", "Regenerate letters", "Re-render a student's letters.", AdminOnly),
        new(StudentsDelete,           "Students", "Delete student", "Soft-delete a student record.", AdminOnly),
        new(StudentsRemoveProgramme,  "Students", "Remove programme from student", "Irreversibly remove an enrolment. Tightened to SuperAdmin by prior decision.", SuperOnly),
        new(StudentsExport,           "Students", "Export students", "Export the students list to CSV (Sales scoped).", AllLevels),

        // Letters
        new(LetterTypesManage, "Letters", "Manage letter types", "Create, edit and delete letter-type definitions.", SuperOnly),
        new(LettersGenerate,   "Letters", "Generate letters", "Generate an approval letter.", AllLevels),
        new(LettersEmailEdit,  "Letters", "Edit letter emails", "Edit dynamic letter-email templates.", EditorPlus),

        // CRM / Mail / Statistics
        new(CrmView,            "CRM & Mail", "View CRM", "Open the CRM (Sales scoped to own leads).", AllLevels),
        new(CrmSettings,        "CRM & Mail", "Edit CRM settings", "Change CRM configuration.", AdminOnly),
        new(MailAccountsManage, "CRM & Mail", "Manage mail accounts", "Add or remove mail accounts and mail access.", SuperOnly),
        new(MailSettings,       "CRM & Mail", "Edit mail settings", "Change global and per-school mail settings.", AdminOnly),
        new(StatisticsView,     "Statistics", "View statistics", "Open the statistics area (Sales sees Signups only).", AllLevels),
        new(StatisticsFinancial, "Statistics", "View financial figures", "See financial figures and the full leaderboard.", SuperOnly),

        // Partners / Programmes / Invoices
        new(PartnersAssignSales,    "Partners", "Assign Sales staff", "Assign Sales staff to a partner.", AdminOnly),
        new(ProgrammesEditDuration, "Programmes", "Edit programme duration", "Edit a programme's duration range.", AdminOnly),
        new(InvoicesMarkPaid,       "Invoices", "Mark invoice paid", "Mark a partner invoice paid (Sales excluded).", ExceptSales),
        new(InvoicesDelete,         "Invoices", "Delete / revert invoice", "Delete an invoice or revert it to unpaid.", SuperOnly),
    ];

    public static bool IsValidKey(string? key) => key is not null && Catalog.Any(p => p.Key == key);
}
