using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>Configurable list of letter languages (System Config). English
/// is the built-in default and is not a row here; each listed language lets
/// a template carry an extra version.</summary>
public class LetterLanguage : IDeletedAtEntity
{
    public Guid LetterLanguageId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}
