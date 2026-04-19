using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class StudentDocument : IDeletedAtEntity
{
    public Guid StudentDocumentId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = default!;
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedByUserId { get; set; }
    public ApplicationUser? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
