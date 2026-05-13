using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// A student's prior academic qualification. Pathways may specify a
/// <c>MinimumEducationLevel</c>; the wizard hides pathways whose minimum
/// rank exceeds the student's chosen <c>HighestDegree</c>.
///
/// <see cref="Rank"/> is the ordinal used for ≥ comparisons (higher = more
/// advanced). <see cref="DisplayOrder"/> controls dropdown ordering when two
/// levels share the same rank (e.g. discipline-specific masters).
/// </summary>
public class EducationLevel : IDeletedAtEntity
{
    public Guid EducationLevelId { get; set; }
    public string Name { get; set; } = default!;
    public int Rank { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}
