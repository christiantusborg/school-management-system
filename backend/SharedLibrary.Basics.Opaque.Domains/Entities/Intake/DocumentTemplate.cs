using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>How a <see cref="DocumentTemplate"/> produces its file (ported verbatim from QuVian core).</summary>
public enum DocumentStrategy
{
    Generate = 0,
    Overlay = 1,
    AcroFormFill = 2,
    /// <summary>Visual designer (Konva). MappingJson carries the layout object.</summary>
    CanvasDesign = 3,
}

/// <summary>
/// A document template + its strategy + its visual field-mapping (ported
/// from QuVian core, single-tenant). <see cref="MappingJson"/> is the
/// visual mapper output (questionnaire field id → PDF field /
/// {{placeholder}}); the base file lives in the 1:1
/// <see cref="DocumentTemplateAsset"/> row.
/// </summary>
public class DocumentTemplate : IDeletedAtEntity
{
    public Guid DocumentTemplateId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public DocumentStrategy Strategy { get; set; }
    public string? BaseAssetRef { get; set; }
    public string MappingJson { get; set; } = "{}";
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Base asset attached 1:1 to a <see cref="DocumentTemplate"/> (the PDF to
/// overlay or the AcroForm to fill). Authoring configuration, not student
/// PII, so stored as plain bytes; re-uploading replaces the row.
/// </summary>
public class DocumentTemplateAsset : IEntity
{
    public Guid DocumentTemplateAssetId { get; set; } = Guid.NewGuid();
    public Guid DocumentTemplateId { get; set; }
    public string Filename { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public DocumentTemplate DocumentTemplate { get; set; } = null!;
}

/// <summary>
/// Reusable authoring-time image (logos, letterheads, signature stamps)
/// referenced by document templates. Intentionally not encrypted.
/// </summary>
public class DocumentTemplateImage : IDeletedAtEntity
{
    public Guid DocumentTemplateImageId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public string DataBase64 { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
