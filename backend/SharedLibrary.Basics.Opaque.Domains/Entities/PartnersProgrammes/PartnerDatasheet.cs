using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A reusable datasheet TEMPLATE (ported from core's PageDefinition concept):
/// extra structured data the Admission Office can keep on partners. Unlike
/// core, the structure is fully relational — sections and fields are rows,
/// and the filled-in data lands in row/value tables, never in JSON blobs.
/// </summary>
public class PartnerDatasheetDefinition : IDeletedAtEntity
{
    public const string AccessHidden = "hidden";
    public const string AccessView = "view";
    public const string AccessEdit = "edit";

    /// <summary>Ordinary datasheet template (Partner Datasheets config).</summary>
    public const string ScopeDatasheet = "datasheet";
    /// <summary>THE single Faculty Profile Information structure — same
    /// builder/engine, but surfaced as the Faculties feature, never in the
    /// datasheet UIs.</summary>
    public const string ScopeFaculty = "faculty";

    public Guid PartnerDatasheetDefinitionId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = ScopeDatasheet;
    /// <summary>Partner-portal access to sheets of this definition:
    /// "hidden" (admin only), "view" (partner sees read-only), or "edit"
    /// (partner may create sheets and fill the fields flagged PartnerCanEdit).</summary>
    public string PartnerAccess { get; set; } = AccessHidden;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One section of a definition: either a "fields" section (single set of
/// label/value pairs) or a "grid" section (a table whose columns are this
/// section's fields and whose lines are PartnerDatasheetRows).
/// </summary>
public class PartnerDatasheetSection : IDeletedAtEntity
{
    public const string KindFields = "fields";
    public const string KindGrid = "grid";

    public Guid PartnerDatasheetSectionId { get; set; } = Guid.NewGuid();
    public Guid PartnerDatasheetDefinitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = KindFields;
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One field of a section — a single field in a "fields" section or a column
/// in a "grid" section. Soft-deleting a field HIDES it everywhere but keeps
/// every stored value; re-adding (restoring) it brings the data back.
/// </summary>
public class PartnerDatasheetField : IDeletedAtEntity
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
    /// <summary>System-generated id, e.g. "MGW-ALC-FAC-{partner}-{n}" —
    /// pattern in OptionsText, sequence counted per (field, partner).</summary>
    public const string TypeAutoId = "autoid";
    /// <summary>System-combined value from sibling fields by label,
    /// e.g. "{First name} {Last name}" — template in OptionsText.</summary>
    public const string TypeComputed = "computed";

    public Guid PartnerDatasheetFieldId { get; set; } = Guid.NewGuid();
    public Guid PartnerDatasheetSectionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = TypeText;
    /// <summary>Dropdown options (one per line) for "select"; the pattern /
    /// template for "autoid" / "computed".</summary>
    public string? OptionsText { get; set; }
    /// <summary>Optional help text shown as an ⓘ tooltip next to the field.</summary>
    public string? Tooltip { get; set; }
    public bool IsRequired { get; set; }
    /// <summary>Partner users may fill this field (definitions with
    /// PartnerAccess "edit"). System types and MGW-only checkboxes stay false.</summary>
    public bool PartnerCanEdit { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One attached sheet on one partner. The same definition may be attached
/// any number of times, each with its own title ("Meeting 12 Jun 2026").
/// </summary>
public class PartnerDatasheet : IDeletedAtEntity
{
    /// <summary>A normal single sheet.</summary>
    public const string KindStandalone = "standalone";
    /// <summary>A repeatable container (core's Group): holds many Item
    /// sheets of one definition, has no data rows of its own.</summary>
    public const string KindGroup = "group";
    /// <summary>One entry inside a Group, e.g. one teacher.</summary>
    public const string KindItem = "item";

    public Guid PartnerDatasheetId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid PartnerDatasheetDefinitionId { get; set; }
    public string Kind { get; set; } = KindStandalone;
    /// <summary>Set on Items: the Group this item belongs to. Loose
    /// reference (no FK) to avoid a self-referencing cascade.</summary>
    public Guid? ParentPartnerDatasheetId { get; set; }
    /// <summary>On Groups: whether partner users may add items below this
    /// group from the partner portal (the definition must also be
    /// partner-editable). Admin-controlled per group.</summary>
    public bool PartnerCanAddItems { get; set; }

    /// <summary>Faculty profiles only: the teacher USER (partner user with
    /// IsTeacher) this profile belongs to. Teachers are accounts, not sheets
    /// — the sheet merely stores the profile data for that user.</summary>
    public string? TeacherUserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One data row of a sheet: "fields" sections have exactly one row per sheet,
/// "grid" sections one row per table line. Deleting a grid line is a plain
/// row delete — no blob rewriting.
/// </summary>
public class PartnerDatasheetRow : IDeletedAtEntity
{
    public Guid PartnerDatasheetRowId { get; set; } = Guid.NewGuid();
    public Guid PartnerDatasheetId { get; set; }
    public Guid PartnerDatasheetSectionId { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One cell: the value of one field on one row. Text stored raw, dates as
/// ISO yyyy-MM-dd, numbers invariant, yes/no as "true"/"false"; file uploads
/// keep the storage path in Value and the original name in FileName.
/// </summary>
public class PartnerDatasheetValue
{
    public Guid PartnerDatasheetValueId { get; set; } = Guid.NewGuid();
    public Guid PartnerDatasheetRowId { get; set; }
    public Guid PartnerDatasheetFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
