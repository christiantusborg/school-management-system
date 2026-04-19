namespace SharedLibrary.Basics.Opaque.Domains;

public enum CaseFileAccessMode
{
    /// <summary>Levels 1 through MinLevel can all decrypt the file.</summary>
    Hierarchical,

    /// <summary>Only exactly MinLevel can decrypt the file.</summary>
    Independent
}
