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
        string? rawToken = null;
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            rawToken = authHeader["Bearer ".Length..].Trim();
        else if (HttpMethods.IsGet(Request.Method) && IsBrowserDownloadPath(Request.Path)
            && Request.Query.TryGetValue("access_token", out var queryToken))
            // File downloads navigate the browser directly (no way to attach a
            // header), so ONLY these statistics export paths may carry the
            // session token as a query parameter.
        {
            rawToken = queryToken.ToString();
            // Base64 session tokens contain '+', which query-string decoding
            // turns into a space if any hop re-encodes the URL. Repair it.
            if (rawToken.Contains(' ')) rawToken = rawToken.Replace(' ', '+');
        }
        if (!string.IsNullOrEmpty(rawToken) && IsBrowserDownloadPath(Request.Path))
            Logger.LogInformation("[stats-dl] auth on download path {Path}: token via {Source}, len={Len}, fp={Head}..{Tail}",
                Request.Path.Value, string.IsNullOrEmpty(authHeader) ? "query" : "header",
                rawToken!.Length, rawToken[..Math.Min(4, rawToken.Length)], rawToken[Math.Max(0, rawToken.Length - 4)..]);
        if (string.IsNullOrEmpty(rawToken))
            return AuthenticateResult.NoResult();

        var session = await tokenService.ValidateTokenAsync(rawToken);
        if (session is null)
        {
            if (IsBrowserDownloadPath(Request.Path))
                Logger.LogWarning("[stats-dl] token REJECTED on {Path} (len={Len})", Request.Path.Value, rawToken.Length);
            return AuthenticateResult.Fail("Invalid or expired token.");
        }

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

    private static bool IsBrowserDownloadPath(PathString path) =>
        (path.StartsWithSegments("/v1/admin/statistics", StringComparison.OrdinalIgnoreCase)
            && (path.Value!.EndsWith("/export", StringComparison.OrdinalIgnoreCase)
                || path.Value.EndsWith("/full-report", StringComparison.OrdinalIgnoreCase)))
        || (path.StartsWithSegments("/v1/admin/overview", StringComparison.OrdinalIgnoreCase)
            && path.Value!.EndsWith("/file", StringComparison.OrdinalIgnoreCase));
}
