using Odin.Api.Base.Authentication;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Resolves the X-Wizard-Token header against `WizardSessionService`. Returns
/// the bound (UserId, StudentId) tuple or null when missing/invalid. Refreshes
/// the cache TTL on success so an active wizard session never expires
/// mid-flow.
/// </summary>
internal static class WizardTokenAuth
{
    public const string HeaderName = "X-Wizard-Token";

    public static async Task<WizardSessionService.WizardSession?> ResolveAsync(
        HttpContext http, WizardSessionService wizard)
    {
        if (!http.Request.Headers.TryGetValue(HeaderName, out var values))
            return null;
        var token = values.ToString();
        if (string.IsNullOrEmpty(token)) return null;

        var session = await wizard.ResolveAsync(token);
        if (session is null) return null;

        await wizard.RefreshAsync(token);
        return session;
    }

    public static IResult Unauthorised() =>
        Results.Json(new { error = "Wizard session expired or missing." }, statusCode: 401);
}
