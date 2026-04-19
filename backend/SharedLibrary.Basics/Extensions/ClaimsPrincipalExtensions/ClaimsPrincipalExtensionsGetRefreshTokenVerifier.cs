using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetRefreshTokenVerifier
{
    /// <summary>
    /// Retrieves the RefreshTokenVerifier from the ClaimsPrincipal using the "RefreshTokenVerifier" claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The RefreshTokenVerifier as a string, or an empty string if not found.</returns>
    public static string GetRefreshTokenVerifier(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst("RefreshTokenVerifier");
        return claim?.Value ?? string.Empty;
    }
}