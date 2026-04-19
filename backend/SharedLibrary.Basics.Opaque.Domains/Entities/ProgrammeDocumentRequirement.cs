using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ProgrammeDocumentRequirement : IDeletedAtEntity
{
    public Guid ProgrammeDocumentRequirementId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Programme Programme { get; set; } = default!;
    public Guid? MajorId { get; set; }
    public Major? Major { get; set; }
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = default!;
    public bool IsRequired { get; set; }
    public DateTime? DeletedAt { get; set; }
}
