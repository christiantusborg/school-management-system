namespace SharedLibrary.Basics.Opaque.Domains;

public class EnrollmentStatusNote
{
    public Guid EnrollmentStatusNoteId { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }

    /// <summary>
    /// The status the enrolment moved INTO at this point in time. Nullable
    /// only because legacy rows pre-date this column; new writes always set it.
    /// </summary>
    public Guid? StatusId { get; set; }

    public string Note { get; set; } = default!;
    public Guid ByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public EnrollmentStatus? Status { get; set; }
}

