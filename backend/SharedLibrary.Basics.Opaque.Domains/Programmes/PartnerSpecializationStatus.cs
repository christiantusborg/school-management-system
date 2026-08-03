namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Workflow state for a single specialization of a partner-owned programme.
/// Approval happens at THIS level; the programme's own
/// <see cref="PartnerProgrammeStatus"/> is derived from its specs (live once
/// at least one spec is approved). Core-programme specializations have no
/// row and are implicitly Approved. Rows are keyed by SpecializationId.
/// </summary>
public class PartnerSpecializationStatus
{
    public Guid SpecializationId { get; set; }

    /// <summary>0 = Draft, 1 = Pending, 2 = Approved, 3 = Rejected.</summary>
    public int Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Specialization Specialization { get; set; } = default!;
}
