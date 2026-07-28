namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Sales-staff ↔ partner assignment (many-to-many): a Sales login only sees
/// the partners assigned to them (plus students they personally added or
/// handle); one partner may carry several sales staff. Managed by
/// Administrator level and above. Visibility only — commission attribution
/// stays per student (Added by / Handled by).
/// </summary>
public class SalesPartnerAssignment
{
    public Guid SalesPartnerAssignmentId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public Guid PartnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
