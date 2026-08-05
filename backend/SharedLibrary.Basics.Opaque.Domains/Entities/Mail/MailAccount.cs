using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Mail;

/// <summary>
/// A mailbox the hub syncs (IMAP) and sends from (SMTP). Only SuperAdmin
/// creates accounts; a per-account access list decides which admission
/// users may open it. Credentials are field-encrypted at rest. Color gives
/// the CLEAR visual label of which account a mail belongs to.
/// </summary>
public class MailAccount : IDeletedAtEntity
{
    public Guid MailAccountId { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string? Color { get; set; }

    public string ImapHost { get; set; } = default!;
    public int ImapPort { get; set; } = 993;
    public bool ImapUseSsl { get; set; } = true;
    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string Username { get; set; } = default!;
    /// <summary>AES-encrypted via FieldEncryption; never returned by APIs.</summary>
    public byte[] PasswordEncrypted { get; set; } = default!;

    public bool IsEnabled { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MailAccountAccess> Access { get; set; } = new List<MailAccountAccess>();
}

/// <summary>Admission user granted access to open a mail account.</summary>
public class MailAccountAccess
{
    public Guid MailAccountAccessId { get; set; } = Guid.NewGuid();
    public Guid MailAccountId { get; set; }
    public Guid UserId { get; set; }

    public MailAccount Account { get; set; } = default!;
}

/// <summary>IMAP folder with the incremental sync cursor (UIDVALIDITY +
/// last seen UID). A UIDVALIDITY change resets the folder.</summary>
public class MailFolder
{
    public Guid MailFolderId { get; set; } = Guid.NewGuid();
    public Guid MailAccountId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsSent { get; set; }
    public long UidValidity { get; set; }
    public long LastSeenUid { get; set; }

    public MailAccount Account { get; set; } = default!;
}

/// <summary>
/// One synced (or hub-sent) email. Stored once, linked to any number of
/// students / CRM leads / partners via <see cref="MailMessageLink"/>.
/// IsRead is the hub-local read flag; the mail server is never modified.
/// </summary>
public class MailMessage
{
    public Guid MailMessageId { get; set; } = Guid.NewGuid();
    public Guid MailAccountId { get; set; }
    public Guid? MailFolderId { get; set; }
    /// <summary>IMAP UID within the folder; 0 for hub-sent mail.</summary>
    public long ImapUid { get; set; }
    public string? MessageIdHeader { get; set; }
    public string? InReplyTo { get; set; }

    public string? Subject { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? ToAddresses { get; set; }
    public string? CcAddresses { get; set; }
    public DateTime? SentAt { get; set; }

    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }

    /// <summary>True for mail we sent (Sent folder or composed in the hub).</summary>
    public bool IsOutbound { get; set; }
    public bool IsRead { get; set; }
    public DateTime SyncedAt { get; set; }

    public MailAccount Account { get; set; } = default!;
    public MailFolder? Folder { get; set; }
    public ICollection<MailAttachment> Attachments { get; set; } = new List<MailAttachment>();
    public ICollection<MailMessageLink> Links { get; set; } = new List<MailMessageLink>();
}

/// <summary>Attachment stored with the mail (≤ 10 MB; larger ones keep only
/// metadata with Skipped = true).</summary>
public class MailAttachment
{
    public Guid MailAttachmentId { get; set; } = Guid.NewGuid();
    public Guid MailMessageId { get; set; }
    public string FileName { get; set; } = default!;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public byte[]? Content { get; set; }
    public bool Skipped { get; set; }

    public MailMessage Message { get; set; } = default!;
}

/// <summary>Auto-link of a mail to a Student, CRM lead or Partner — exactly
/// one of the three ids is set; ALL matches are linked.</summary>
public class MailMessageLink
{
    public Guid MailMessageLinkId { get; set; } = Guid.NewGuid();
    public Guid MailMessageId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? CrmLeadId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? MatchedAddress { get; set; }

    public MailMessage Message { get; set; } = default!;
}
