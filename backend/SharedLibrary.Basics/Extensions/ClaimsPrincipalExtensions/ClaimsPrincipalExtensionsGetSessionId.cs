using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetSessionId
{
    /// <summary>
    /// Retrieves the SessionId as a Guid from the ClaimsPrincipal using the "SessionId" claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The parsed Guid or Guid.Empty if parsing fails.</returns>
    public static Guid GetSessionId(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst("SessionId");
        return claim is not null && Guid.TryParse(claim.Value, out var sessionId)
            ? sessionId
            : Guid.Empty;
    }
}