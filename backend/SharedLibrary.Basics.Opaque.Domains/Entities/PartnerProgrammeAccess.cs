using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class PartnerProgrammeAccess : IDeletedAtEntity
{
    public Guid PartnerProgrammeAccessId { get; set; } = Guid.NewGuid();

    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = default!;

    public Guid ProgrammeId { get; set; }
    public Programme Programme { get; set; } = default!;

    public Guid MajorId { get; set; }
    public Major Major { get; set; } = default!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedByUserId { get; set; }

    public bool DisabledByPartner { get; set; }

    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public DateTime? DeletedAt { get; set; }
}
