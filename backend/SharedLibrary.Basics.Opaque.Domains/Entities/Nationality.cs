using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Nationality : IDeletedAtEntity
{
    public int NationalityId { get; set; }
    public string Code { get; set; } = default!;   // ISO 3166-1 alpha-2 (uppercase, 2 chars)
    public string Name { get; set; } = default!;   // Country name
    public DateTime? DeletedAt { get; set; }
}
