using System.Security.Claims;

namespace QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;

/// <summary>
/// Provides extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensionsGetOrganizationId
{
    /// <summary>
    /// Retrieves the OrganizationId as a Guid from the ClaimsPrincipal using the "OrganizationId" claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal instance.</param>
    /// <returns>The parsed Guid or Guid.Empty if parsing fails.</returns>
    public static Guid GetOrganizationId(this ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        var claim = principal.FindFirst("OrganizationId");
        return claim is not null && Guid.TryParse(claim.Value, out var organizationId)
            ? organizationId
            : Guid.Empty;
    }
}