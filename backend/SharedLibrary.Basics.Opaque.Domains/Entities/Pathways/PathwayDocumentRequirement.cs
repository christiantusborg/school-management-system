using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class PathwayDocumentRequirement : IDeletedAtEntity
{
    public Guid PathwayDocumentRequirementId { get; set; } = Guid.NewGuid();
    
    public Guid PathwayId { get; set; }
    public Guid DocumentTypeId { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public Pathway Pathway { get; set; } = default!;
    public DocumentType DocumentType { get; set; } = default!;
    
    
}
