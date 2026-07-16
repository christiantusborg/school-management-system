using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A reusable questionnaire definition authored in the drag-and-drop builder
/// (ported from the QuVian core intake plane, single-tenant: no TenantId,
/// GroupId or firm-library split). <see cref="DefinitionJson"/> is the full
/// Questionnaire JSON the builder edits and the renderer fills.
/// <see cref="DefinitionHash"/> is the SHA-256 (uppercase hex) of
/// DefinitionJson, stamped onto every submitted response for the
/// "what version did they answer?" record.
/// </summary>
public class QuestionnaireTemplate : IDeletedAtEntity
{
    public Guid QuestionnaireTemplateId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Version { get; set; } = "1.0.0";
    public string DefinitionJson { get; set; } = null!;
    public string DefinitionHash { get; set; } = null!;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
