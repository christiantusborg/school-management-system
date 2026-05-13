using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class UserLanguage : IDeletedAtEntity
{
    public Guid StudentLanguageId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int LanguageId { get; set; }
    public Language Language { get; set; } = default!;
    public LanguageProficiency Proficiency { get; set; }
    public DateTime? DeletedAt { get; set; }
}

