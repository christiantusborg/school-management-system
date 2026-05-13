using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerContractNote : IDeletedAtEntity
{
    public Guid PartnerContractNoteId { get; set; } = Guid.NewGuid();
    public Guid PartnerContractId { get; set; }
    public PartnerContract Contract { get; set; } = default!;

    public string Body { get; set; } = default!;

    public string AuthorUserId { get; set; } = default!;
    public ApplicationUser Author { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}