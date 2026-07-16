using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>What kind of artifact an <see cref="IntakeOutput"/> represents (PDF/JSON/CSV; DOCX deferred, as in core).</summary>
public enum IntakeOutputKind
{
    Pdf = 0,
    Json = 1,
    Csv = 2,
}

/// <summary>
/// A file produced by the generate step of an intake run. Adapted from
/// QuVian core: instead of an encrypted case SharedFile, IBSS stores the
/// artifact via IFileStorage and keeps the path here (same pattern as
/// letters and student documents). Signing/classification columns from
/// core's E2E plane are intentionally dropped.
/// </summary>
public class IntakeOutput : IDeletedAtEntity
{
    public Guid IntakeOutputId { get; set; } = Guid.NewGuid();
    public Guid IntakeResponseId { get; set; }
    public Guid? DocumentTemplateId { get; set; }
    public IntakeOutputKind OutputKind { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public IntakeResponse IntakeResponse { get; set; } = null!;
    public DocumentTemplate? DocumentTemplate { get; set; }
}
