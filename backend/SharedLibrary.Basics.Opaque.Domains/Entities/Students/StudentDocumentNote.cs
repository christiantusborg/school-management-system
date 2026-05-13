namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Append-only audit-log row recording every state change on a
/// <see cref="StudentDocument"/>. The current state is denormalised onto
/// <c>StudentDocument.CurrentStatusId</c>; full history lives here.
///
/// <para>
/// <c>Note</c> is the free-text reason supplied by the actor (typically only
/// populated when status is RejectedByPartner / RejectedByEnrolment).
/// </para>
/// </summary>
public class StudentDocumentNote
{
    public Guid StudentDocumentNoteId { get; set; } = Guid.NewGuid();
    public Guid StudentDocumentId { get; set; }
    public Guid StatusId { get; set; }
    public string ByUserId { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public StudentDocument StudentDocument { get; set; } = default!;
    public DocumentStatus Status { get; set; } = default!;
}
