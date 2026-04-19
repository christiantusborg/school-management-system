using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class FinalProjectStatus : IDeletedAtEntity
{
    public int FinalProjectStatusId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool AllowSetByPartner { get; set; }
    public DateTime? DeletedAt { get; set; }
}
