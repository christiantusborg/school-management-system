using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class PathwayDocumentRequirement : IDeletedAtEntity
{
    public Guid PathwayDocumentRequirementId { get; set; } = Guid.NewGuid();
    public int PathwayId { get; set; }
    public Pathway Pathway { get; set; } = default!;
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
