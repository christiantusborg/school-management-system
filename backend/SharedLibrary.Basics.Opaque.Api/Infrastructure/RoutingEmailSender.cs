using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.Email;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// The app's <see cref="IEmailSender"/>. Every send first looks for a school
/// transport: letter emails carry the school on the message; system mail (MFA
/// codes, verification links — the 3-arg overload) resolves it from the
/// recipient's latest enrolment. A school whose Provider is "Gmail"/"Smtp"
/// sends through its own transport, falling back to the default flow when the
/// school send fails. The default flow is the global System Config → Email
/// settings (Gmail/SMTP) or the built-in Brevo transport. Settings are read
/// per-send so admins can switch transports without a redeploy.
/// </summary>
internal sealed class RoutingEmailSender(
    BrevoEmailSender brevo,
    OdinDbContext db,
    ILogger<RoutingEmailSender> logger) : IEmailSender, ISchoolMailTester
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Student-facing system mail routes through the recipient's school
        // transport when one is configured; anyone else gets the built-in path.
        var schoolId = await ResolveSchoolByRecipientAsync(to, cancellationToken);
        if (schoolId is not null)
        {
            var school = await LoadSchoolSettingsAsync(schoolId.Value, cancellationToken);
            if (school is not null)
            {
                try
                {
                    await SendViaSchoolAsync(school, new EmailMessage(to, subject, body), cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Mail] School {SchoolId} transport failed for system mail to {To}; falling back to built-in", schoolId, to);
                }
            }
        }
        await brevo.SendAsync(to, subject, body, cancellationToken);
    }

    public async Task SendAsync(EmailMessage msg, CancellationToken cancellationToken = default)
    {
        if (msg.SchoolId is Guid sid)
        {
            var school = await LoadSchoolSettingsAsync(sid, cancellationToken);
            if (school is not null)
            {
                try
                {
                    await SendViaSchoolAsync(school, msg, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Mail] School {SchoolId} transport failed for {To}; falling back to default", sid, msg.To);
                }
            }
        }
        await SendViaDefaultAsync(msg, cancellationToken);
    }

    /// <summary>Test send strictly through the school's own transport — no fallback.</summary>
    public async Task SendTestAsync(Guid schoolId, string to, CancellationToken ct)
    {
        var school = await LoadSchoolSettingsAsync(schoolId, ct);
        if (school is null)
            throw new InvalidOperationException("This school has no mail service of its own configured (Provider is Default or credentials are incomplete). Save Gmail or SMTP settings first.");
        await SendViaSchoolAsync(school, new EmailMessage(
            To: to,
            Subject: "MGW school mail configuration test",
            HtmlBody: "<p>This is a test email using this school's own mail settings. If you received it, the school's outbound mail is configured correctly.</p>"), ct);
    }

    /// <summary>Returns the school's settings only when it has a usable own transport.</summary>
    private async Task<SchoolMailSettings?> LoadSchoolSettingsAsync(Guid schoolId, CancellationToken ct)
    {
        var s = await db.SchoolMailSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId, ct);
        if (s is null) return null;
        var usable = s.Provider switch
        {
            "Gmail" => !string.IsNullOrWhiteSpace(s.GmailServiceAccountJson)
                       && !string.IsNullOrWhiteSpace(s.GmailImpersonatedUser),
            "Smtp"  => !string.IsNullOrWhiteSpace(s.SmtpHost),
            _       => false,
        };
        return usable ? s : null;
    }

    /// <summary>Recipient email → student → latest enrolment → programme's school.</summary>
    private async Task<Guid?> ResolveSchoolByRecipientAsync(string to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to)) return null;
        var normalized = to.Trim().ToUpperInvariant();
        return await db.Students
            .Where(s => s.User.NormalizedEmail == normalized)
            .SelectMany(s => db.Enrollments
                .Where(e => e.StudentId == s.StudentId && e.DeletedAt == null))
            .OrderByDescending(e => e.CommencementDate ?? DateTime.MinValue)
            .Select(e => db.Specializations
                .Where(sp => sp.SpecializationId == e.SpecializationId)
                .Select(sp => sp.Programmes.SchoolId)
                .FirstOrDefault())
            .FirstOrDefaultAsync(ct);
    }

    private static async Task SendViaSchoolAsync(SchoolMailSettings s, EmailMessage msg, CancellationToken ct)
    {
        var fromEmail = msg.FromEmail ?? s.FromEmail;
        var fromName  = msg.FromName  ?? s.FromName;

        if (s.Provider == "Gmail")
        {
            var gmailFrom = fromEmail ?? s.GmailImpersonatedUser!;
            var outgoing = msg with { FromEmail = gmailFrom, FromName = fromName };
            await GmailEmailDispatcher.SendAsync(
                s.GmailServiceAccountJson!, s.GmailImpersonatedUser!,
                outgoing, gmailFrom, fromName ?? "Admission Team", ct);
            return;
        }

        var smtpFrom = fromEmail ?? s.SmtpUsername ?? string.Empty;
        var smtpOutgoing = msg with { FromEmail = smtpFrom, FromName = fromName };
        await SmtpEmailDispatcher.SendAsync(
            s.SmtpHost!, s.SmtpPort ?? 587, s.SmtpUsername, s.SmtpPassword, s.SmtpSecurity,
            smtpOutgoing, smtpFrom, fromName ?? "Admission Team", ct);
    }

    private async Task SendViaDefaultAsync(EmailMessage msg, CancellationToken cancellationToken)
    {
        var settings = await db.MailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        var fromEmail = msg.FromEmail ?? settings?.FromEmail;
        var fromName  = msg.FromName  ?? settings?.FromName;

        var useGmail = settings is { Provider: "Gmail" }
            && !string.IsNullOrWhiteSpace(settings.GmailServiceAccountJson)
            && !string.IsNullOrWhiteSpace(settings.GmailImpersonatedUser);

        if (useGmail)
        {
            // Gmail forces the From to be the impersonated mailbox (or a
            // verified alias); fall back to it when no explicit From is set.
            var gmailFrom = fromEmail ?? settings!.GmailImpersonatedUser!;
            var outgoing = msg with { FromEmail = gmailFrom, FromName = fromName ?? settings!.FromName };
            await GmailEmailDispatcher.SendAsync(
                settings!.GmailServiceAccountJson!, settings.GmailImpersonatedUser!,
                outgoing, gmailFrom, fromName ?? "Admission Team", cancellationToken);
            return;
        }

        var useSmtp = settings is { Provider: "Smtp" } && !string.IsNullOrWhiteSpace(settings.SmtpHost);
        if (useSmtp)
        {
            var smtpFrom = fromEmail ?? settings!.SmtpUsername ?? string.Empty;
            var outgoing = msg with { FromEmail = smtpFrom, FromName = fromName ?? settings!.FromName };
            await SmtpEmailDispatcher.SendAsync(
                settings!.SmtpHost!, settings.SmtpPort ?? 587, settings.SmtpUsername, settings.SmtpPassword, settings.SmtpSecurity,
                outgoing, smtpFrom, fromName ?? settings.FromName ?? "Admission Team", cancellationToken);
            return;
        }

        var brevoOutgoing = (fromEmail is not null || fromName is not null)
            ? msg with { FromEmail = fromEmail, FromName = fromName }
            : msg;
        await brevo.SendAsync(brevoOutgoing, cancellationToken);
    }
}
