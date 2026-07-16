using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// A configurable "current position by function" option shown in the signup
/// wizard's Background step (e.g. Consulting, Human Resources, Others). Managed
/// in System Config → Position Functions. Soft-deletable.
/// </summary>
public class PositionFunction : IDeletedAtEntity
{
    public Guid PositionFunctionId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}
