namespace Odin.Api.Base.Authorization;

/// <summary>
/// The four access states a role can hold for a controllable item (Phase 2).
/// Ordered so a numeric compare works: a write needs <see cref="Edit"/>, a read
/// needs at least <see cref="View"/>.
/// </summary>
public enum AccessLevel
{
    /// <summary>Not rendered at all — the item does not exist for this role.</summary>
    Hidden = 0,
    /// <summary>Shown as present but locked; the server rejects reads and writes.</summary>
    NoAccess = 1,
    /// <summary>Read-only: sees the value, every control disabled.</summary>
    View = 2,
    /// <summary>Full read + write.</summary>
    Edit = 3,
}

/// <summary>What kind of thing a catalogue item is (drives which states apply).</summary>
public enum ResourceType
{
    /// <summary>A whole tab.</summary>
    Tab,
    /// <summary>A section / panel within a surface.</summary>
    Section,
    /// <summary>An individual field (4 states apply).</summary>
    Field,
    /// <summary>A button / action (only Edit = allowed vs Hidden/NoAccess = denied).</summary>
    Action,
}

public static class AccessLevels
{
    public static bool IsValid(int v) => v is >= 0 and <= 3;

    public static string Name(AccessLevel l) => l switch
    {
        AccessLevel.Edit => "edit",
        AccessLevel.View => "view",
        AccessLevel.NoAccess => "no_access",
        _ => "hidden",
    };
}
