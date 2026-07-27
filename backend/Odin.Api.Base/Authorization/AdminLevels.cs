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
    /// <summary>Sales staff: same permissions as Viewer — read-only admission
    /// access, mainly for sharing personal referral signup links.</summary>
    public const string Sales              = "Sales";

    public static readonly string[] All =
    [
        SuperAdministrator,
        Administrator,
        Manager,
        Editor,
        Viewer,
        Sales
    ];

    public static bool IsValid(string? level) => level is not null && Array.IndexOf(All, level) >= 0;
}
