using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// One RUN of one module at one partner (a cohort/section): every
/// Programme→Specialization→Module runs in intervals, and partners set these
/// schedules up ahead of time, assign a teacher and their admitted students,
/// upload teaching materials and grading sheets, and track QA. The cohort
/// number is generated once from the configurable pattern
/// ("{partner}-{module}-{n}", sequence per partner+module).
/// </summary>
public class ModuleCohort : IDeletedAtEntity
{
    public Guid ModuleCohortId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    /// <summary>Required cohort type — brings the type's extra-data form.</summary>
    public Guid? CohortTypeId { get; set; }
    public Guid ProgrammeId { get; set; }
    /// <summary>The module (Subject).</summary>
    public Guid SubjectId { get; set; }
    /// <summary>Assigned teacher (Faculties feature); optional.</summary>
    public Guid? TeacherId { get; set; }

    public string CohortNumber { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Admin override; effective due date = this ?? EndDate + 1 month.</summary>
    public DateTime? GradingSheetDueOverride { get; set; }
    public DateTime? GradingSheetUploadedDate { get; set; }

    public bool DocQaChecked { get; set; }
    public DateTime? DocQaDate { get; set; }
    public bool GradeQaChecked { get; set; }
    public DateTime? GradeQaDate { get; set; }

    // Grading-sheet reminder bookkeeping (one email per stage).
    public bool Reminder2WeeksSent { get; set; }
    public bool Reminder1WeekSent { get; set; }
    public bool ReminderOverdueSent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>A student (enrolment) assigned to a cohort. The cohort's
/// "Number of Students Enrolled" is the live count of these rows.</summary>
public class ModuleCohortStudent : IDeletedAtEntity
{
    public Guid ModuleCohortStudentId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortId { get; set; }
    public Guid StudentEnrollmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Configurable upload fields of the cohort record (System Config → Module
/// Cohorts builder): label + single/multiple files. Seeded with the five
/// agreed Teaching Materials fields.
/// </summary>
public class CohortUploadField : IDeletedAtEntity
{
    public Guid CohortUploadFieldId { get; set; } = Guid.NewGuid();
    /// <summary>Owning cohort type — every type carries its own upload
    /// fields (null only on legacy rows before types owned uploads).</summary>
    public Guid? CohortTypeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    /// <summary>Grading-sheet fields set GradingSheetUploadedDate on upload.</summary>
    public bool IsGradingSheet { get; set; }
    /// <summary>Students may download this field's files on the cohorts they
    /// are assigned to (e.g. "Module Outline Given to Students").</summary>
    public bool VisibleToStudents { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One uploaded file on one cohort's upload field.</summary>
public class CohortUploadFile : IDeletedAtEntity
{
    public Guid CohortUploadFileId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortId { get; set; }
    public Guid CohortUploadFieldId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

/// <summary>Single-row settings of the Module Cohorts feature.</summary>
public class ModuleCohortSettings
{
    public Guid ModuleCohortSettingsId { get; set; } = Guid.NewGuid();
    /// <summary>Cohort number pattern; {partner}, {module} and {n} expand.</summary>
    public string CohortNumberPattern { get; set; } = "{partner}-{module}-{n}";
}

/// <summary>
/// A cohort TYPE (System Config → Module Cohorts): each type carries its own
/// extra-data form (fields built like the faculty profile builder) that the
/// cohort's Record tab shows for filling. Multiple types; one is required
/// per cohort and may be changed later — entered values are kept per field.
/// </summary>
public class CohortType : IDeletedAtEntity
{
    public Guid CohortTypeId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One field of a cohort type's form. Soft-deleted fields keep
/// their stored values; re-adding the same label restores them.</summary>
public class CohortTypeField : IDeletedAtEntity
{
    public const string TypeText = "text";
    public const string TypeNumber = "number";
    public const string TypeDate = "date";
    public const string TypeSelect = "select";
    public const string TypeBool = "bool";
    public const string TypeFile = "file";
    /// <summary>Static text written in the TEMPLATE only; cohorts show it
    /// read-only (instructions, notes). Content lives in OptionsText.</summary>
    public const string TypeInfo = "info";

    public Guid CohortTypeFieldId { get; set; } = Guid.NewGuid();
    public Guid CohortTypeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = TypeText;
    /// <summary>Dropdown options for "select", one per line.</summary>
    public string? OptionsText { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One value of one cohort-type field on one cohort. File fields
/// keep the storage path in Value and the original name in FileName.</summary>
public class CohortFieldValue
{
    public Guid CohortFieldValueId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortId { get; set; }
    public Guid CohortTypeFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
