namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// One historical render of a released letter. The live StudentDocuments row
/// always serves the LATEST PDF (its id and download links stay stable);
/// every (re)generation also appends a row here. Browsable by student,
/// partner and admission. v1 populates this for config-created letter types
/// only; built-ins join at migration.
/// </summary>
public class StudentDocumentVersion
{
    public Guid StudentDocumentVersionId { get; set; } = Guid.NewGuid();
    public Guid StudentDocumentId { get; set; }
    /// <summary>1, 2, 3… per document.</summary>
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    /// <summary>"Manual" | "StatusTrigger" | "PassMark" | "SpecChange".</summary>
    public string Trigger { get; set; } = string.Empty;
    public string? GeneratedByName { get; set; }
    public string? GeneratedByUserId { get; set; }
    /// <summary>Language rendered; null = English (default).</summary>
    public string? Language { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
