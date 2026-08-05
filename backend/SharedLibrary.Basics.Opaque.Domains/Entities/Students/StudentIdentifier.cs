namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// One Student ID belonging to a student — students can carry SEVERAL
/// (legacy numbers, per-intake IDs, imported IDs). Exactly one row per
/// student is primary and is mirrored into Student.StudentNumber (what
/// lists, letters and the ID card print). Values are globally unique across
/// all students. Managed by Admission only; partners and students see the
/// list read-only. Switching primary does NOT re-render released PDFs.
/// </summary>
public class StudentIdentifier
{
    public Guid StudentIdentifierId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public string Value { get; set; } = default!;
    /// <summary>Optional origin note ("Legacy IBAS ID", "CSV import", …).</summary>
    public string? Label { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    public Student Student { get; set; } = default!;
}
