using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// A configurable "current employment industry" option shown in the signup
/// wizard's Background step (e.g. Consulting, Healthcare, Others). Managed in
/// System Config → Employment Industries. Soft-deletable.
/// </summary>
public class EmploymentIndustry : IDeletedAtEntity
{
    public Guid EmploymentIndustryId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}
