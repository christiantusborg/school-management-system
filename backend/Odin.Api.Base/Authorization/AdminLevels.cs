namespace Odin.Api.Base.Authorization;

/// <summary>
/// Admin privilege levels. Every admin user holds the base <c>Admin</c> role
/// (so existing <c>AdminOnly</c> authorization continues to work) plus exactly
/// one of these level roles. Managing admin users themselves requires
/// <see cref="SuperAdministrator"/> — enforced via the <c>SuperAdminOnly</c>
/// policy.
/// </summary>
public static class AdminLevels
{
    public const string SuperAdministrator = "SuperAdministrator";
    public const string Administrator      = "Administrator";
    public const string Manager            = "Manager";
    public const string Editor             = "Editor";
    public const string Viewer             = "Viewer";

    public static readonly string[] All =
    [
        SuperAdministrator,
        Administrator,
        Manager,
        Editor,
        Viewer
    ];

    public static bool IsValid(string? level) => level is not null && Array.IndexOf(All, level) >= 0;
}
