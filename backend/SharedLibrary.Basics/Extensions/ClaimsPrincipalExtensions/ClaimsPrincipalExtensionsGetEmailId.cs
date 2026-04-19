using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetEmailId
{
    /// <summary>
    /// Retrieves the EmailId as a Guid from the ClaimsPrincipal using ClaimTypes.Email.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The parsed Guid or Guid.Empty if parsing fails.</returns>
    public static Guid GetEmailId(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst(ClaimTypes.Email);
        return claim is not null && Guid.TryParse(claim.Value, out var emailId)
            ? emailId
            : Guid.Empty;
    }
}