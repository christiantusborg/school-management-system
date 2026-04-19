using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class TuitionFeeStatus : IDeletedAtEntity
{
    public int TuitionFeeStatusId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
