using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Pathway : IDeletedAtEntity
{
    public int PathwayId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
