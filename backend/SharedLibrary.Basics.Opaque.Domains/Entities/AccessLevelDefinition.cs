using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Global seed table — 6 fixed access levels used across all cases.
/// Level 1 is highest privilege (Lead Partner); Level 6 is lowest (External Consultant).
/// </summary>
public class AccessLevelDefinition : IEntity
{
    /// <summary>1–6, primary key. Never changes after seeding.</summary>
    public int Level { get; set; }

    /// <summary>Default display name, e.g. "Lead Partner". Can be overridden per case via CaseLevelLabelOverride.</summary>
    public required string Name { get; set; }

    public string? Description { get; set; }
}
