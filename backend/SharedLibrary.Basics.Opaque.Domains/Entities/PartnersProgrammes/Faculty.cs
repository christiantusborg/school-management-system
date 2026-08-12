using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Faculty Profile Information — its OWN feature, not a datasheet. ONE global
/// structure (sections + typed fields, edited with the same style of builder)
/// defines what every teacher profile contains.
/// </summary>
public class FacultyProfileSection : IDeletedAtEntity
{
    public const string KindFields = "fields";
    public const string KindGrid = "grid";

    public Guid FacultyProfileSectionId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = KindFields;
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One field of the faculty profile structure. Soft-deleting hides it but
/// keeps every stored value; re-adding the same label restores the data.
/// </summary>
public class FacultyProfileField : IDeletedAtEntity
{
    public const string TypeText = "text";
    public const string TypeNumber = "number";
    public const string TypeDate = "date";
    public const string TypeFile = "file";
    /// <summary>Multiple file uploads; value is a JSON array of
    /// {token,name} objects stored in the same Value string.</summary>
    public const string TypeFiles = "files";
    public const string TypeSelect = "select";
    public const string TypeBool = "bool";
    /// <summary>System-generated id, e.g. "MGW-ALC-FAC-{partner}-{n}".</summary>
    public const string TypeAutoId = "autoid";
    /// <summary>System-combined from sibling fields, e.g. "{First name} {Last name}".</summary>
    public const string TypeComputed = "computed";

    public Guid FacultyProfileFieldId { get; set; } = Guid.NewGuid();
    public Guid FacultyProfileSectionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = TypeText;
    /// <summary>Dropdown options (one per line) for "select"; the pattern /
    /// template for "autoid" / "computed".</summary>
    public string? OptionsText { get; set; }
    /// <summary>Optional help text shown as an ⓘ tooltip next to the field.</summary>
    public string? Tooltip { get; set; }
    public bool IsRequired { get; set; }
    /// <summary>Partner users may fill this field; MGW-only fields stay false.</summary>
    public bool PartnerCanEdit { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// A TEACHER: a partner's faculty member. Usually linked to a partner USER
/// with the Teacher role (their portal login); the profile data lives in
/// TeacherProfileRows/Values shaped by the global structure.
/// </summary>
public class Teacher : IDeletedAtEntity
{
    public Guid TeacherId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    /// <summary>The teacher's login (ApplicationUser with IsTeacher). Null
    /// only for profiles migrated before logins existed.</summary>
    public string? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One data row: "fields" sections one per teacher, "grid" sections
/// one per table line.</summary>
public class TeacherProfileRow : IDeletedAtEntity
{
    public Guid TeacherProfileRowId { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public Guid FacultyProfileSectionId { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One cell: text raw, date ISO, number invariant, yes/no
/// "true"/"false"; file uploads keep the storage path in Value and the
/// original name in FileName.</summary>
public class TeacherProfileValue
{
    public Guid TeacherProfileValueId { get; set; } = Guid.NewGuid();
    public Guid TeacherProfileRowId { get; set; }
    public Guid FacultyProfileFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
