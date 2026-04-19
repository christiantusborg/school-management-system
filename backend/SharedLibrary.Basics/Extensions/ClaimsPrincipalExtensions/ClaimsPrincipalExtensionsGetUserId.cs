using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetUserId
{
    /// <summary>
    /// Retrieves the UserId as a Guid from the ClaimsPrincipal using the "userId" claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The parsed Guid or Guid.Empty if parsing fails.</returns>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst("userId");
        return claim is not null && Guid.TryParse(claim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }
}