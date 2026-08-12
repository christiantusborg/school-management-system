namespace Odin.Api.Base.Authorization;

/// <summary>
/// Phase-2 resource catalogue: every controllable item in the admin portal,
/// each with a <see cref="ResourceType"/> and per-role default access levels
/// that reproduce today's behaviour. It is a SUPERSET of the Phase-1 action
/// catalogue (<see cref="AdminPermissions"/>): the 30 Phase-1 keys are folded
/// in as <see cref="ResourceType.Action"/> items keeping their exact current
/// roles; the new field/section/tab items default to Edit for every admin role,
/// because there is no field-level gating today (any admin who can open a
/// surface can edit it) — the value of Phase 2 is that they can now be
/// restricted.
///
/// SuperAdministrator is never listed — it bypasses the matrix (always Edit).
/// A role not listed in EditRoles or ViewRoles defaults to Hidden for that item.
/// </summary>
public static class AdminResources
{
    public sealed record AdminResource(
        string Key, string Surface, string Group, string Label, string Description,
        ResourceType Type, string[] EditRoles, string[] ViewRoles);

    private static readonly string[] All =
        [AdminLevels.Administrator, AdminLevels.Manager, AdminLevels.Editor, AdminLevels.Viewer, AdminLevels.Sales];

    // ── New Phase-2 items (default: Edit for all admin roles = reproduce today) ──
    private static AdminResource F(string key, string surface, string group, string label, string desc, ResourceType type = ResourceType.Field)
        => new(key, surface, group, label, desc, type, All, []);

    private static readonly AdminResource[] NewItems =
    [
        // ── Student drawer: tabs ──────────────────────────────────────────
        F("student.tab.details",   "Student", "Tabs", "Details tab", "Open the student Details tab", ResourceType.Tab),
        F("student.tab.programs",  "Student", "Tabs", "Programs tab", "Open the Programs tab", ResourceType.Tab),
        F("student.tab.documents", "Student", "Tabs", "Documents tab", "Open the Documents tab", ResourceType.Tab),
        F("student.tab.moodle",    "Student", "Tabs", "Moodle tab", "Open the Moodle tab", ResourceType.Tab),
        F("student.tab.mail",      "Student", "Tabs", "Mail tab", "Open the student Mail tab", ResourceType.Tab),
        F("student.tab.log",       "Student", "Tabs", "Log tab", "Open the Log tab", ResourceType.Tab),
        F("student.tab.activity",  "Student", "Tabs", "Activity log tab", "Open the Activity log tab", ResourceType.Tab),
        // Student › Details
        F("student.details.account",  "Student", "Details", "Account section", "Username / email / IDs block", ResourceType.Section),
        F("student.details.email",    "Student", "Details", "Email", "Edit the student's email"),
        F("student.details.reset_password", "Student", "Details", "Reset password", "Reset the student's password", ResourceType.Action),
        F("student.details.personal", "Student", "Details", "Personal details", "Name, DOB, passport, address, nationality", ResourceType.Section),
        F("student.details.background","Student", "Details", "Background", "Degree, experience, languages", ResourceType.Section),
        // Student › Programs › Enrolment
        F("student.enrolment",              "Student", "Enrolment", "Enrolment section", "Programme / specialisation / dates / durations", ResourceType.Section),
        F("student.enrolment.change_status","Student", "Enrolment", "Change status", "Change an enrolment's status", ResourceType.Action),
        F("student.enrolment.cohort_assign","Student", "Enrolment", "Assign module cohorts", "Assign modules to cohorts", ResourceType.Action),
        F("student.enrolment.bulk_agreement","Student", "Enrolment", "Bulk agreement", "Move enrolment onto a bulk agreement", ResourceType.Action),
        // Student › Programs › Grades
        F("student.grades",           "Student", "Grades", "Grades section", "The grade table", ResourceType.Section),
        F("student.grades.enter",     "Student", "Grades", "Enter grade", "Type a module grade", ResourceType.Action),
        F("student.grades.save_draft","Student", "Grades", "Save grade draft", "Save grades without submitting", ResourceType.Action),
        F("student.grades.submit",    "Student", "Grades", "Submit grades", "Submit grades for approval", ResourceType.Action),
        F("student.grades.approve",   "Student", "Grades", "Approve grades", "Approve submitted grades", ResourceType.Action),
        F("student.grades.reject",    "Student", "Grades", "Reject grades", "Send grades back", ResourceType.Action),
        // Student › Programs › Letters
        F("student.letters",          "Student", "Letters", "Letters section", "The letters list", ResourceType.Section),
        F("student.letters.passmark", "Student", "Letters", "Approval pass-mark", "Edit the programme approval pass-mark"),
        F("student.letters.generate", "Student", "Letters", "Generate letter", "Generate / regenerate a letter", ResourceType.Action),
        F("student.letters.download", "Student", "Letters", "Download letter", "Download a letter", ResourceType.Action),
        F("student.letters.history",  "Student", "Letters", "Letter history", "View letter versions", ResourceType.Action),
        F("student.letters.upload",   "Student", "Letters", "Upload letter", "Upload an old letter", ResourceType.Action),
        // Student › Programs › Payment
        F("student.payment",          "Student", "Payment", "Payment section", "Installments and invoices", ResourceType.Section),
        F("student.payment.records",  "Student", "Payment", "Payment records", "Add / delete payment records", ResourceType.Action),
        // Student › Documents / Moodle
        F("student.documents.open",   "Student", "Documents", "Open document", "View a student document", ResourceType.Action),
        F("student.documents.replace","Student", "Documents", "Replace document", "Replace an unverified document", ResourceType.Action),
        F("student.documents.add",    "Student", "Documents", "Add document", "Add an additional document", ResourceType.Action),
        F("student.moodle.settings",  "Student", "Moodle", "Moodle settings", "Edit Moodle enabled / credentials"),

        // ── Module cohorts ────────────────────────────────────────────────
        F("cohorts.list",             "Cohorts", "Cohorts", "Cohorts", "View the module cohorts", ResourceType.Section),
        F("cohorts.manage",           "Cohorts", "Cohorts", "Create / edit cohort", "Create and edit cohorts", ResourceType.Action),
        F("cohorts.files",            "Cohorts", "Cohorts", "Cohort files", "Manage cohort files", ResourceType.Action),
        F("cohorts.grades",           "Cohorts", "Cohorts", "Cohort grades", "Enter cohort grades", ResourceType.Action),
        F("cohorts.aggregate_view",   "Cohorts", "Cohorts", "Aggregate results", "View cohort questionnaire aggregates (3-response anonymity gate still applies)", ResourceType.Action),
        F("cohorts.assign_questionnaire","Cohorts", "Cohorts", "Assign questionnaire", "Attach a questionnaire to a cohort", ResourceType.Action),

        // ── Partner: tabs ─────────────────────────────────────────────────
        F("partner.tab.students",   "Partner", "Tabs", "Students tab", "Partner Students tab", ResourceType.Tab),
        F("partner.tab.cohorts",    "Partner", "Tabs", "Module Cohorts tab", "Partner Module Cohorts tab", ResourceType.Tab),
        F("partner.tab.invoices",   "Partner", "Tabs", "Invoices tab", "Partner Invoices tab", ResourceType.Tab),
        F("partner.tab.import",     "Partner", "Tabs", "Import tab", "Partner Import tab", ResourceType.Tab),
        F("partner.tab.mail",       "Partner", "Tabs", "Mail tab", "Partner Mail tab", ResourceType.Tab),
        F("partner.tab.agreements", "Partner", "Tabs", "Bulk Agreements tab", "Partner Bulk Agreements tab", ResourceType.Tab),
        F("partner.tab.profile",    "Partner", "Tabs", "Profile tab", "Partner Profile ⚙ tab", ResourceType.Tab),
        F("partner.tab.users",      "Partner", "Tabs", "Users tab", "Partner Users ⚙ tab", ResourceType.Tab),
        F("partner.tab.programmes", "Partner", "Tabs", "Programmes tab", "Partner Programmes ⚙ tab", ResourceType.Tab),
        F("partner.tab.documents",  "Partner", "Tabs", "Partnership Documents tab", "Partner Documents ⚙ tab", ResourceType.Tab),
        F("partner.tab.faculty",    "Partner", "Tabs", "Faculty tab", "Partner Faculty ⚙ tab", ResourceType.Tab),
        // Partner › Profile fields
        F("partner.profile.name",           "Partner", "Profile", "Name", "Partner name"),
        F("partner.profile.slug",           "Partner", "Profile", "Slug", "Signup-URL slug"),
        F("partner.profile.contacts",       "Partner", "Profile", "Contacts", "Named contacts"),
        F("partner.profile.address",        "Partner", "Profile", "Address", "Address fields"),
        F("partner.profile.website",        "Partner", "Profile", "Website", "Website"),
        F("partner.profile.registration_no","Partner", "Profile", "Registration no.", "Registration number"),
        F("partner.profile.tax_id",         "Partner", "Profile", "Tax ID", "Tax ID"),
        F("partner.profile.short_code",     "Partner", "Profile", "Short code", "Faculty/datasheet ID short code"),
        F("partner.profile.tier",           "Partner", "Profile", "Tier", "Partnership tier"),
        F("partner.profile.contract",       "Partner", "Profile", "Contract", "Contract start/end"),
        F("partner.profile.internal_notes", "Partner", "Profile", "Internal notes", "Internal notes"),
        // Partner › Users
        F("partner.users.add",           "Partner", "Users", "Add user", "Add a partner user", ResourceType.Action),
        F("partner.users.edit",          "Partner", "Users", "Edit user", "Edit a partner user", ResourceType.Action),
        F("partner.users.reset_password","Partner", "Users", "Reset password", "Reset a partner user's password", ResourceType.Action),
        F("partner.users.disable",       "Partner", "Users", "Disable / enable user", "Disable or enable a partner user", ResourceType.Action),
        F("partner.users.delete",        "Partner", "Users", "Delete user", "Delete a partner user", ResourceType.Action),
        F("partner.users.set_teacher",   "Partner", "Users", "Set Teacher flag", "Mark a user as read-only Teacher", ResourceType.Action),
        // Partner › Programmes
        F("partner.programmes.core_grant",   "Partner", "Programmes", "Grant / revoke core spec", "Toggle a core specialization for the partner", ResourceType.Action),
        F("partner.programmes.add_remove_core","Partner", "Programmes", "Add / remove core programme", "Add or remove a core programme", ResourceType.Action),
        F("partner.programmes.student_card",  "Partner", "Programmes", "Student card toggle", "Toggle digital student card", ResourceType.Action),
        F("partner.programmes.approve",       "Partner", "Programmes", "Approve programme/spec", "Approve a custom programme or specialization", ResourceType.Action),
        F("partner.programmes.reject",        "Partner", "Programmes", "Reject programme/spec", "Reject a custom programme or specialization", ResourceType.Action),
        F("partner.programmes.reopen",        "Partner", "Programmes", "Reopen programme/spec", "Reopen a custom programme or specialization", ResourceType.Action),
        F("partner.programmes.letter_template","Partner", "Programmes", "Edit letter template", "View/edit a per-programme letter template", ResourceType.Action),
        // Partner › Documents / Faculty
        F("partner.documents.add",    "Partner", "Documents", "Add partnership document", "Add a partnership document", ResourceType.Action),
        F("partner.documents.edit",   "Partner", "Documents", "Edit partnership document", "Edit a partnership document", ResourceType.Action),
        F("partner.documents.delete", "Partner", "Documents", "Delete partnership document", "Delete a partnership document", ResourceType.Action),
        F("partner.faculty.list",     "Partner", "Faculty", "Faculty list", "View the faculty list", ResourceType.Section),
        F("partner.faculty.add",      "Partner", "Faculty", "Add teacher", "Add a teacher", ResourceType.Action),
        F("partner.faculty.edit",     "Partner", "Faculty", "Edit teacher profile", "Edit a teacher's profile", ResourceType.Action),
        F("partner.faculty.delete",   "Partner", "Faculty", "Delete teacher", "Delete a teacher", ResourceType.Action),

        // ── System Config: templates & lists ──────────────────────────────
        F("config.partner_doc_types",  "Config", "Templates", "Partnership document types", "Config: partnership document types/designs", ResourceType.Section),
        F("config.datasheet_defs",     "Config", "Templates", "Datasheet definitions", "Config: partner datasheet structures", ResourceType.Section),
        F("config.faculty_structure",  "Config", "Templates", "Faculty profile structure", "Config: faculty profile structure", ResourceType.Section),
        F("config.cohort_types",       "Config", "Templates", "Cohort types", "Config: module cohort types/structure", ResourceType.Section),
        F("config.rubrics",            "Config", "Templates", "Grading rubrics", "Config: grading rubrics", ResourceType.Section),
        F("config.lists",              "Config", "Lists", "Config lists", "Config: document types, education levels, modes, entry requirements, schools, currencies, contact methods, position functions, employment industries", ResourceType.Section),

        // ── Questionnaires ────────────────────────────────────────────────
        F("questionnaires.builder",         "Questionnaires", "Questionnaires", "Builder", "Build/edit questionnaires", ResourceType.Action),
        F("questionnaires.templates",       "Questionnaires", "Questionnaires", "Templates", "Manage questionnaire templates", ResourceType.Action),
        F("questionnaires.assign",          "Questionnaires", "Questionnaires", "Assign", "Assign a questionnaire to an audience", ResourceType.Action),
        F("questionnaires.aggregate_view",  "Questionnaires", "Questionnaires", "View aggregate", "View aggregate (anonymous) statistics", ResourceType.Action),
        F("questionnaires.individual_view", "Questionnaires", "Questionnaires", "View individual responses", "View individual responses", ResourceType.Action),
        F("questionnaires.public_forms",    "Questionnaires", "Questionnaires", "Public forms", "Manage public forms", ResourceType.Action),

        // ── CRM / Mail / misc ─────────────────────────────────────────────
        F("crm.leads_edit",   "CRM & Mail", "CRM", "Edit lead", "Create/edit CRM leads", ResourceType.Action),
        F("crm.leads_delete", "CRM & Mail", "CRM", "Delete lead", "Delete a CRM lead", ResourceType.Action),
        F("crm.leads_stage",  "CRM & Mail", "CRM", "Change lead stage", "Move a lead through stages", ResourceType.Action),
        F("mail.send",        "CRM & Mail", "Mail", "Send mail", "Send an email", ResourceType.Action),
        F("bulk_agreements.manage", "Partners", "Agreements", "Bulk agreements", "Create/assign bulk agreements", ResourceType.Action),
        F("import.students",  "Students", "Import", "Import students", "Bulk-import students", ResourceType.Action),
        F("import.grades",    "Students", "Import", "Import grades", "Bulk-import grades", ResourceType.Action),
    ];

    /// <summary>The full Phase-2 catalogue: Phase-1 actions (unchanged roles)
    /// folded in, plus the new granular items.</summary>
    public static readonly IReadOnlyList<AdminResource> Catalog = BuildCatalog();

    private static List<AdminResource> BuildCatalog()
    {
        var list = new List<AdminResource>();
        foreach (var p in AdminPermissions.Catalog)
            list.Add(new AdminResource(p.Key, p.Area, p.Area, p.Label, p.Description, ResourceType.Action, p.DefaultRoles, []));
        list.AddRange(NewItems);
        return list;
    }

    public static bool IsValidKey(string? key) => key is not null && Catalog.Any(r => r.Key == key);

    /// <summary>The seed access level for a role on an item (reproduce-today).</summary>
    public static AccessLevel DefaultLevel(AdminResource r, string role)
    {
        if (r.EditRoles.Contains(role)) return AccessLevel.Edit;
        if (r.ViewRoles.Contains(role)) return AccessLevel.View;
        return AccessLevel.Hidden;
    }
}
