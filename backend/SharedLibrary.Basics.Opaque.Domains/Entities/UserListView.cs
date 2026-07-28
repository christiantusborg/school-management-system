namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// A user's saved column view for a list page (currently the admission
/// Students overview): which columns show and in what order. Unlimited named
/// views per user; the built-in Default view is not stored.
/// </summary>
public class UserListView
{
    public Guid UserListViewId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    /// <summary>Which list this view belongs to, e.g. "students".</summary>
    public string Page { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One visible column of a view, in display order.</summary>
public class UserListViewColumn
{
    public Guid UserListViewColumnId { get; set; } = Guid.NewGuid();
    public Guid UserListViewId { get; set; }
    public string ColumnKey { get; set; } = default!;
    public int SortOrder { get; set; }
}
