namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// One assignment target for a Targeted <see cref="IntakeInstance"/>.
/// Exactly one of the five ids is set:
/// - StudentId: this specific student
/// - PartnerId: every student of that partner (or, for Partner-audience
///   instances, that partner organisation itself)
/// - ProgrammeId / SpecializationId / SubjectId: every student with an
///   enrolment matching — resolved dynamically at list time, so students who
///   enrol later are covered automatically without re-assigning.
/// </summary>
public class IntakeAssignment
{
    public Guid IntakeAssignmentId { get; set; } = Guid.NewGuid();
    public Guid IntakeInstanceId { get; set; }

    public Guid? StudentId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? ProgrammeId { get; set; }
    public Guid? SpecializationId { get; set; }
    public Guid? SubjectId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IntakeInstance Instance { get; set; } = null!;
}
