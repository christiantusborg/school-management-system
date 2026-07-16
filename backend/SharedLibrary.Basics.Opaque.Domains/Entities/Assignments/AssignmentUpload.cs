using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Assignments;

/// <summary>
/// An assignment file uploaded for an enrolment's module (Programme →
/// Specialization → Subject). Students upload their own work from the
/// student portal; partners and the Admission Office can upload on their
/// behalf. Every upload carries a required human title shown next to the
/// download button. Teachers/staff/students discuss it via
/// <see cref="AssignmentComment"/>s.
/// </summary>
public class AssignmentUpload : IDeletedAtEntity
{
    public Guid AssignmentUploadId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public Guid SubjectId { get; set; }

    /// <summary>Required document title entered by the uploader.</summary>
    public string Title { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public string StoragePath { get; set; } = default!;

    /// <summary>"Student" | "Partner" | "Teacher" | "Admission Office".</summary>
    public string UploadedByRole { get; set; } = default!;
    public string? UploadedByName { get; set; }
    public string? UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<AssignmentComment> Comments { get; set; } = [];
}

/// <summary>
/// One chat-style comment on an uploaded assignment — the conversation
/// between teachers, partner staff, the Admission Office and the student.
/// </summary>
public class AssignmentComment
{
    public Guid AssignmentCommentId { get; set; } = Guid.NewGuid();
    public Guid AssignmentUploadId { get; set; }

    /// <summary>"Student" | "Partner" | "Teacher" | "Admission Office".</summary>
    public string AuthorRole { get; set; } = default!;
    public string? AuthorName { get; set; }
    public string? AuthorUserId { get; set; }

    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AssignmentUpload Upload { get; set; } = default!;
}
