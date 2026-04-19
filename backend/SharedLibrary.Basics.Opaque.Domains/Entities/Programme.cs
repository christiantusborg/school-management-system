using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Programme : IDeletedAtEntity
{
    public Guid ProgrammeId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public DateTime? DeletedAt { get; set; }

    // Partner ownership. Null = IBSS core programme. Non-null = partner-owned clone or from-scratch.
    public Guid? PartnerId { get; set; }
    public Partner? Partner { get; set; }

    // If cloned from a core programme, reference to its source. Null for from-scratch or core programmes.
    public Guid? ClonedFromProgrammeId { get; set; }
    public Programme? ClonedFromProgramme { get; set; }

    // Approval workflow. Core programmes are seeded as Approved.
    public ProgrammeStatus Status { get; set; } = ProgrammeStatus.Approved;

    // Partner-controlled active switch. Only meaningful when Status == Approved. Core programmes default true.
    public bool IsActive { get; set; } = true;

    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? CreatedByUserId { get; set; }

    // Admin override: disables a partner programme regardless of status. Core programmes stay false.
    public bool IsDisabledByAdmin { get; set; }

    public ICollection<Major> Majors { get; set; } = [];
}
