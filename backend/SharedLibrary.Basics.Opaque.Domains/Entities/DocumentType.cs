using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class DocumentType : IDeletedAtEntity
{
    public int DocumentTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }
}
