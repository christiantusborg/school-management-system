namespace Odin.Api.Base.Email;

/// <summary>
/// Per-school outbound-mail configuration edited in System Config → Schools →
/// Edit. When a student-facing email is tied to a school (via the enrolment's
/// programme) and that school's Provider is "Gmail" or "Smtp", mail is sent
/// through this transport; on failure, or when Provider is "Default", the
/// global <see cref="MailSettings"/> transport is used instead. Secrets are
/// encrypted at rest exactly like the global settings.
/// </summary>
public class SchoolMailSettings
{
    public Guid SchoolMailSettingsId { get; set; } = Guid.NewGuid();

    /// <summary>The awarding school this configuration belongs to. Unique.</summary>
    public Guid SchoolId { get; set; }

    /// <summary>"Default" (use the global settings), "Gmail" or "Smtp".</summary>
    public string Provider { get; set; } = "Default";

    /// <summary>Raw Google service-account JSON key. Encrypted at rest.</summary>
    public string? GmailServiceAccountJson { get; set; }

    /// <summary>Workspace user the service account impersonates.</summary>
    public string? GmailImpersonatedUser { get; set; }

    // ── Custom SMTP ──────────────────────────────────────────────────────
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    /// <summary>SMTP password. Encrypted at rest.</summary>
    public string? SmtpPassword { get; set; }
    /// <summary>"Auto" | "StartTls" | "SslOnConnect" | "None".</summary>
    public string? SmtpSecurity { get; set; }

    /// <summary>Visible From address on this school's emails.</summary>
    public string? FromEmail { get; set; }

    /// <summary>Visible From display name on this school's emails.</summary>
    public string? FromName { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}

/// <summary>
/// Sends a test email strictly through a school's own configured transport,
/// with no fallback to the default settings, so the test proves the school's
/// configuration itself. Throws on failure. Implemented by the app host's
/// routing email sender.
/// </summary>
public interface ISchoolMailTester
{
    /// <summary>Throws when the school has no own transport configured or the send fails.</summary>
    Task SendTestAsync(Guid schoolId, string to, CancellationToken ct);
}
