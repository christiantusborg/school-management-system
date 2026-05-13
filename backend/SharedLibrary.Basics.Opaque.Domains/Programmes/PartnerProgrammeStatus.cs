namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Workflow state for a partner-owned programme. Rows exist only for
/// programmes a partner created (cloned or from-scratch); core programmes
/// owned by IBSS admin have no row and are implicitly Approved+Active.
/// </summary>
public class PartnerProgrammeStatus
{
    public Guid ProgrammeId { get; set; }

    /// <summary>0 = Draft, 1 = Pending, 2 = Approved, 3 = Rejected.</summary>
    public int Status { get; set; }
    public bool IsActive { get; set; }
    public bool IsDisabledByAdmin { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Programme Programme { get; set; } = default!;
}
