using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Authentication;

public class SessionTokenAuthenticationHandler(
    IOptionsMonitor<SessionTokenOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SessionTokenService tokenService,
    UserManager<ApplicationUser> userManager)
    : AuthenticationHandler<SessionTokenOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawToken = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(rawToken))
            return AuthenticateResult.NoResult();

        var session = await tokenService.ValidateTokenAsync(rawToken);
        if (session is null)
            return AuthenticateResult.Fail("Invalid or expired token.");

        var user = session.User;
        if (!user.IsEnabled)
            return AuthenticateResult.Fail("Account is disabled.");

        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new("token", rawToken)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
