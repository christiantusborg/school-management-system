using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class EnrollmentDocument : IDeletedAtEntity
{
    public Guid EnrollmentDocumentId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment StudentEnrollment { get; set; } = default!;
    public Guid StudentDocumentId { get; set; }
    public StudentDocument StudentDocument { get; set; } = default!;
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = default!;
    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? DeletedAt { get; set; }
}
