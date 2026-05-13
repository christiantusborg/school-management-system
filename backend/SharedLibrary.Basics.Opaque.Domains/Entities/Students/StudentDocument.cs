using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class StudentDocument : IDeletedAtEntity
{
    public Guid StudentDocumentId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid DocumentTypeId { get; set; }

    /// <summary>
    /// Per-application linkage. Each row is uniquely scoped to one
    /// <see cref="Enrollment"/> after submit — the same student applying
    /// to BBA and MBA gets two rows for the same passport, fully
    /// independent. Nullable only because the signup wizard accepts
    /// uploads BEFORE enrolments are materialised at /submit; in that
    /// window <see cref="SignupSpecializationId"/> tracks the intended
    /// destination, then /submit fills in <c>EnrollmentId</c>.
    /// </summary>
    public Guid? EnrollmentId { get; set; }

    /// <summary>
    /// Wizard-only marker — which specialization the student picked when
    /// they uploaded this document. Cleared once <see cref="EnrollmentId"/>
    /// is filled in at /submit; null on rows created post-signup.
    /// </summary>
    public Guid? SignupSpecializationId { get; set; }

    public string FileName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Storage-relative key as returned by <c>IFileStorage.SaveAsync</c>
    /// (e.g. "{studentId}/{guid}-{filename}"). Null on legacy rows that
    /// pre-date this column; the serve endpoint falls back to a directory
    /// scan in that case.
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Current state of the document. Full history lives in
    /// <see cref="StudentDocumentNote"/>; this column is the latest-state
    /// cache that list/projection queries read.
    /// </summary>
    public Guid CurrentStatusId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Student Student { get; set; } = default!;
    public DocumentType DocumentType { get; set; } = default!;
    public DocumentStatus CurrentStatus { get; set; } = default!;
    public Enrollment? Enrollment { get; set; }
}
