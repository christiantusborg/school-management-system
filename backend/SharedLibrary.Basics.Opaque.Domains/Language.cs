using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Language : IDeletedAtEntity
{
    public int LanguageId { get; set; }
    public string Code { get; set; } = default!;   // ISO 639-1 (lowercase, 2 chars)
    public string Name { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
