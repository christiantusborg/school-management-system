using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class EnrollmentStatus : IDeletedAtEntity
{
    public int EnrollmentStatusId { get; set; }
    public string Name { get; set; } = default!;
    public bool AllowSetByPartner { get; set; }
    public DateTime? DeletedAt { get; set; }
}
