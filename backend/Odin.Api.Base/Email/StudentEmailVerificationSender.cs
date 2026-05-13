using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.TransientStateCache;

namespace Odin.Api.Base.Email;

public sealed record VerifyEmailState(string Token, string Email, string UserId, DateTime CreatedAt);

public sealed class StudentEmailVerificationSender(
    IEmailSender emailSender,
    ITransientStateCache cache,
    IConfiguration config)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    public async Task<string> IssueAndSendAsync(string userId, string email, CancellationToken ct)
    {
        var token = GenerateToken();
        await cache.SetAsync($"verify-email:{userId}", new VerifyEmailState(token, email, userId, DateTime.UtcNow), Ttl);

        var origin = config["App:StudentOrigin"] ?? "http://localhost:5173";
        var link = $"{origin.TrimEnd('/')}/#/apply/verify-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";

        var body =
            "Welcome to IBSS — please confirm your email and finish your application.\n\n" +
            "Click the link below to confirm your email. You can also use it any time to " +
            "return to your application and continue where you left off:\n\n" +
            $"{link}\n\n" +
            "If you did not request this, you can ignore this email.";

        await emailSender.SendAsync(email, "Verify your IBSS application email", body, ct);
        return token;
    }

    /// <summary>
    /// Idempotent — the same link works for repeated visits during the wizard. Token stays
    /// valid for the cache TTL so the applicant can always click "confirm" to resume.
    /// </summary>
    public async Task<VerifyEmailState?> ValidateAsync(string userId, string token)
    {
        var state = await cache.GetAsync<VerifyEmailState>($"verify-email:{userId}");
        if (state is null) return null;
        return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(state.Token),
                System.Text.Encoding.UTF8.GetBytes(token))
            ? state : null;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
