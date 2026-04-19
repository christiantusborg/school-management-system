using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetIat
{
    /// <summary>
    /// Retrieves the 'iat' (Issued At) claim value as a long from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The Iat value as a long. Returns 0 if the claim is not found or is invalid.</returns>
    public static long GetIat(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst("iat");
        return claim is not null && long.TryParse(claim.Value, out var iat)
            ? iat
            : 0;
    }
}