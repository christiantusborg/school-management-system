using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A rich-text merge template (ported from QuVian core; IBSS single-tenant,
/// no case-local split). <see cref="BodyJson"/> holds the editor document;
/// merge tokens ({{field_id}}) bind to questionnaire field ids at
/// generation time.
/// </summary>
public class TextTemplate : IDeletedAtEntity
{
    public Guid TextTemplateId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string BodyJson { get; set; } = null!;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
