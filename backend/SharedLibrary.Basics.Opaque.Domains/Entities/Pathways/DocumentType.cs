using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class DocumentType : IDeletedAtEntity
{
    public Guid DocumentTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>
    /// True for document types that the system itself produces (e.g. generated
    /// letter PDFs). These are hidden from the application-document upload UI
    /// and managed by the letter-release pipeline rather than admins.
    /// </summary>
    public bool IsSystemGenerated { get; set; }

    public DateTime? DeletedAt { get; set; }
    public ICollection<DocumentTypeVerifyRequirement> DocumentTypeVerifyRequirements { get; set; } = new List<DocumentTypeVerifyRequirement>();
}

public class DocumentTypeVerifyRequirement : IDeletedAtEntity
{
    public Guid DocumentTypeVerifyRequirementId { get; set; } = Guid.NewGuid();
    public Guid DocumentTypeId { get; set; }

    /// <summary>Positive checklist label shown in the WHAT TO VERIFY column (e.g. "Photo is legible").</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Negative phrasing used when this requirement isn't met and the
    /// reviewer rejects the document (e.g. "Photo unclear"). Falls back
    /// to <see cref="Name"/> when null.
    /// </summary>
    public string? RejectionLabel { get; set; }

    public DateTime? DeletedAt { get; set; }
    public DocumentType DocumentType { get; set; } = default!;
}
