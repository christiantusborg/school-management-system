namespace Odin.Api.Base.Email;

/// <summary>
/// Singleton row holding the outbound-mail configuration that an admin edits
/// in the portal (System Config → Email). The Gmail service-account key is
/// stored encrypted at rest via a field converter. Letter emails route to
/// Gmail when <see cref="Provider"/> is "Gmail" and the service account is
/// present; otherwise they fall back to the built-in (Brevo) transport.
/// </summary>
public class MailSettings
{
    /// <summary>Fixed singleton key — there is only ever one row.</summary>
    public static readonly Guid SingletonId = new("11111111-1111-1111-1111-111111111111");

    public Guid MailSettingsId { get; set; } = SingletonId;

    /// <summary>"Gmail", "Smtp" or "Brevo". Selects the letter-email transport.</summary>
    public string Provider { get; set; } = "Brevo";

    /// <summary>Raw Google service-account JSON key. Encrypted at rest.</summary>
    public string? GmailServiceAccountJson { get; set; }

    /// <summary>Workspace user the service account impersonates (e.g. admissions@ibss.edu.eu).</summary>
    public string? GmailImpersonatedUser { get; set; }

    // ── Custom SMTP (e.g. SiteGround) ────────────────────────────────────
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    /// <summary>SMTP password. Encrypted at rest.</summary>
    public string? SmtpPassword { get; set; }
    /// <summary>"Auto" | "StartTls" | "SslOnConnect" | "None".</summary>
    public string? SmtpSecurity { get; set; }

    /// <summary>Visible From address on letter emails.</summary>
    public string? FromEmail { get; set; }

    /// <summary>Visible From display name on letter emails.</summary>
    public string? FromName { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
