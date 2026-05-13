using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Lookup table for the current state of a <see cref="StudentDocument"/>.
/// Seeded with five rows: Submitted, VerifiedByPartner, VerifiedByEnrolment,
/// RejectedByPartner, RejectedByEnrolment. Lookup-table rather than enum so
/// new states can be added without a code change.
/// </summary>
public class DocumentStatus : IDeletedAtEntity
{
    public Guid DocumentStatusId { get; set; } = Guid.NewGuid();

    /// <summary>Stable machine code (e.g. "Submitted"). Unique.</summary>
    public string Code { get; set; } = default!;

    /// <summary>Human-readable label shown in UI.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Where this status sits in the flow. Higher = more advanced.</summary>
    public int Level { get; set; }

    /// <summary>
    /// Floor: the minimum level the document can revert to from this
    /// status. Reserved for the future state-machine guard.
    /// </summary>
    public int LevelDown { get; set; }

    public DateTime? DeletedAt { get; set; }
}
