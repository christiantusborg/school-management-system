using System.Security.Cryptography;
using SharedLibrary.Basics.TransientStateCache;

namespace Odin.Api.Base.Authentication;

/// <summary>
/// Long-lived signup-wizard tokens stored out-of-band from regular session tokens.
/// A wizard token only authorises /v1/public/draft-signup/* endpoints — it cannot be
/// used to log in or reach any dashboard. Lasts 7 days, refreshed on every save.
/// </summary>
public sealed class WizardSessionService(ITransientStateCache cache)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    public sealed record WizardSession(string UserId, Guid StudentId);

    public async Task<string> IssueAsync(string userId, Guid studentId)
    {
        var token = GenerateToken();
        await cache.SetAsync($"wizard:{token}", new WizardSession(userId, studentId), Ttl);
        return token;
    }

    public async Task<WizardSession?> ResolveAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        return await cache.GetAsync<WizardSession>($"wizard:{token}");
    }

    public async Task RefreshAsync(string token)
    {
        var existing = await ResolveAsync(token);
        if (existing is not null)
            await cache.SetAsync($"wizard:{token}", existing, Ttl);
    }

    public async Task RevokeAsync(string token)
    {
        if (!string.IsNullOrEmpty(token))
            await cache.RemoveAsync($"wizard:{token}");
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
