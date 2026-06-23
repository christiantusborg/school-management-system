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

        var htmlBody =
            "<p>Welcome to IBSS — please confirm your email and finish your application.</p>" +
            "<p>Click the button below to confirm your email. You can also use it any time to " +
            "return to your application and continue where you left off:</p>" +
            $"<p><a href=\"{link}\" style=\"display:inline-block;padding:10px 18px;background:#1a4d8c;" +
            "color:#fff;text-decoration:none;border-radius:6px;font-weight:600;\">Confirm my email</a></p>" +
            $"<p>Or paste this link into your browser:<br><a href=\"{link}\">{link}</a></p>" +
            "<p style=\"color:#6b7888;font-size:13px;\">If you did not request this, you can ignore this email.</p>";

        // Route through the same transport the admin configured for letter
        // emails (Gmail/SMTP/Brevo) via the EmailMessage overload, instead of
        // the always-Brevo simple overload. The From identity is resolved by
        // the transport from the portal mail settings.
        await emailSender.SendAsync(
            new EmailMessage(To: email, Subject: "Verify your IBSS application email", HtmlBody: htmlBody),
            ct);
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
