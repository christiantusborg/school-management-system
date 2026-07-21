using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// One entry in the immutable per-student note log written manually by the
/// Admission Office or partner staff (distinct from <see cref="StudentNote"/>,
/// which is the system audit trail). Two levels: a general note on the
/// student (<see cref="StudentEnrollmentId"/> null) or a note on one of the
/// programmes/specializations they signed up for (enrolment set).
///
/// Immutability rules: title/content are never edited or deleted. Visibility
/// may only be WIDENED, and only by the authoring side: partner notes are
/// always visible to the Admission Office and the partner (not changeable),
/// and the partner may additionally open its own notes to the student;
/// admission notes default to admission-only and admission may open them to
/// partner and/or student. Partners can never change what admission set.
/// </summary>
public class StudentLogNote : IEntity
{
    public Guid StudentLogNoteId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }

    /// <summary>Null = general note on the student; set = note on that
    /// specific enrolment (programme/specialization).</summary>
    public Guid? StudentEnrollmentId { get; set; }

    public string Title { get; set; } = "";
    public string Content { get; set; } = default!;

    /// <summary>"Admission" or "Partner".</summary>
    public string AuthorRole { get; set; } = default!;
    public string? AuthorName { get; set; }
    public string? AuthorUserId { get; set; }

    public bool VisibleToPartner { get; set; }
    public bool VisibleToStudent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
