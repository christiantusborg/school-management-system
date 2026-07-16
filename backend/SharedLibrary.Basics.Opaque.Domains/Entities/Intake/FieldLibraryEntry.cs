using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A reusable field/section building block for the questionnaire builder
/// (ported from QuVian core's firm-wide field library; IBSS is
/// single-tenant so no TenantId). Drag one of these into any questionnaire
/// to insert the contained group or items from <see cref="DefinitionJson"/>.
/// </summary>
public class FieldLibraryEntry : IDeletedAtEntity
{
    public Guid FieldLibraryEntryId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Category { get; set; } = "general";
    public string DefinitionJson { get; set; } = null!;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
