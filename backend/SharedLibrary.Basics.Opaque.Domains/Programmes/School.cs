using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// An awarding school/institution a programme belongs to. Shown as a suffix on
/// the programme name in the portal (e.g. "Bachelor of Business Administration
/// (IBSS)"). Every programme (core and partner-owned) references exactly one
/// school. Soft-deletable; a school in use by any programme cannot be deleted.
/// </summary>
public class School : IDeletedAtEntity
{
    public Guid SchoolId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Programme> Programmes { get; set; } = [];
}
