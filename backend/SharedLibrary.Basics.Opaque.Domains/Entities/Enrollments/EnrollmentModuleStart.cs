namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Per-student override of when one module (subject) starts within an
/// enrolment. No row = the default: the enrolment's commencement date.
/// Two modes, toggleable in the admin UI:
/// - explicit date (<see cref="UseOffset"/> = false, <see cref="StartDate"/> set)
/// - commencement + N days (<see cref="UseOffset"/> = true, <see cref="OffsetDays"/> set);
///   the resolved date follows automatically when commencement is edited.
/// Set/changed by the Admission Office only; partner and student see the
/// resolved dates read-only.
/// </summary>
public class EnrollmentModuleStart
{
    public Guid EnrollmentModuleStartId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public Guid SubjectId { get; set; }

    public bool UseOffset { get; set; }
    public DateTime? StartDate { get; set; }
    public int? OffsetDays { get; set; }

    /// <summary>End-date override, same dual mode as start. A start override
    /// may exist without an end override and vice versa; the row is deleted
    /// only when neither remains.</summary>
    public bool EndUseOffset { get; set; }
    public DateTime? EndDate { get; set; }
    public int? EndOffsetDays { get; set; }
}
