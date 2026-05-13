using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Lookup row for the lifecycle state of an <see cref="Enrollment"/>.
/// Same shape as <see cref="DocumentStatus"/>: a stable <see cref="Code"/>
/// plus a <see cref="Level"/> + <see cref="LevelDown"/> pair that drives
/// the multi-actor approval flow (Student → Partner → Admission Office).
/// </summary>
public class EnrollmentStatus : IDeletedAtEntity
{
    public Guid EnrollmentStatusId { get; set; } = Guid.NewGuid();

    /// <summary>Stable machine code (e.g. "Submitted", "ReviewedByPartner"). Unique.</summary>
    public string Code { get; set; } = default!;

    /// <summary>Human-readable label shown in UI.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Where this status sits in the flow. Higher = more advanced.</summary>
    public int Level { get; set; }

    /// <summary>
    /// Floor: the minimum level the enrolment can revert to from this
    /// status. Used by the future state-machine guard to reject illegal
    /// downgrades. Not enforced yet.
    /// </summary>
    public int LevelDown { get; set; }

    /// <summary>
    /// Who acts next while the enrolment sits at this status. One of
    /// "Student" / "Partner" / "Admission". Null on terminal states.
    /// </summary>
    public string? NextActionRole { get; set; }

    /// <summary>
    /// The status the enrolment auto-transitions to when the current
    /// owner completes their happy-path action. Null on terminal
    /// states. Branching transitions (e.g. partner reject vs approve)
    /// are still expressed in handler code; this field is the default
    /// "complete" target.
    /// </summary>
    public Guid? NextStatusOnCompleteId { get; set; }
    public EnrollmentStatus? NextStatusOnComplete { get; set; }

    public DateTime? DeletedAt { get; set; }
}
