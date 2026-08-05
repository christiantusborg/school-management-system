using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using Odin.Api.Base.Crypto;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains.Mail;
using MailFolder = SharedLibrary.Basics.Opaque.Domains.Mail.MailFolder;

namespace Odin.Api.Base.Mail;

/// <summary>
/// Webmail engine: IMAP sync (all folders, 90-day initial window, UID
/// cursor per folder, attachments ≤ 10 MB), automatic linking of every mail
/// to matching students / CRM leads / partners by counterpart address, and
/// SMTP send through the account's real mail server. The mail server is
/// treated read-only — the hub never deletes or flags server messages.
/// </summary>
public sealed class MailHubService(IServiceScopeFactory scopeFactory, ILogger<MailHubService> logger)
{
    public const long MaxAttachmentBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan InitialWindow = TimeSpan.FromDays(90);
    private static readonly SemaphoreSlim SyncGate = new(1, 1);

    public async Task<(int Synced, string? Error)> SyncAccountAsync(Guid accountId, CancellationToken ct)
    {
        await SyncGate.WaitAsync(ct);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
            var account = await db.MailAccounts.FirstOrDefaultAsync(a => a.MailAccountId == accountId && a.DeletedAt == null, ct);
            if (account is null) return (0, "Account not found.");
            try
            {
                var count = await SyncCoreAsync(db, account, ct);
                account.LastSyncAt = DateTime.UtcNow;
                account.LastSyncError = null;
                await db.SaveChangesAsync(ct);
                return (count, null);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mail sync failed for {Account}", account.EmailAddress);
                account.LastSyncError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                await db.SaveChangesAsync(CancellationToken.None);
                return (0, ex.Message);
            }
        }
        finally { SyncGate.Release(); }
    }

    private async Task<int> SyncCoreAsync(OdinDbContext db, MailAccount account, CancellationToken ct)
    {
        var password = System.Text.Encoding.UTF8.GetString(FieldEncryption.Decrypt(account.PasswordEncrypted));
        using var imap = new ImapClient();
        await imap.ConnectAsync(account.ImapHost, account.ImapPort, account.ImapUseSsl, ct);
        await imap.AuthenticateAsync(account.Username, password, ct);

        var synced = 0;
        var personal = imap.GetFolder(imap.PersonalNamespaces[0]);
        var folders = (await personal.GetSubfoldersAsync(true, ct)).ToList();
        if (imap.Inbox is not null && folders.All(f => f.FullName != imap.Inbox.FullName)) folders.Insert(0, imap.Inbox);

        foreach (var folder in folders)
        {
            ct.ThrowIfCancellationRequested();
            if (folder.Attributes.HasFlag(FolderAttributes.NonExistent) || folder.Attributes.HasFlag(FolderAttributes.NoSelect)) continue;
            try { await folder.OpenAsync(FolderAccess.ReadOnly, ct); }
            catch { continue; }

            var isSent = folder.Attributes.HasFlag(FolderAttributes.Sent)
                || folder.FullName.Contains("sent", StringComparison.OrdinalIgnoreCase);
            var isJunkOrTrash = folder.Attributes.HasFlag(FolderAttributes.Junk)
                || folder.Attributes.HasFlag(FolderAttributes.Trash)
                || folder.FullName.Contains("spam", StringComparison.OrdinalIgnoreCase);
            if (isJunkOrTrash) { await folder.CloseAsync(false, ct); continue; }

            var row = await db.MailFolders.FirstOrDefaultAsync(f =>
                f.MailAccountId == account.MailAccountId && f.Name == folder.FullName, ct);
            if (row is null)
            {
                row = new MailFolder { MailAccountId = account.MailAccountId, Name = folder.FullName, IsSent = isSent, UidValidity = folder.UidValidity };
                db.MailFolders.Add(row);
                await db.SaveChangesAsync(ct);
            }
            if (row.UidValidity != folder.UidValidity)
            {
                // Folder was rebuilt server-side: restart its cursor.
                row.UidValidity = folder.UidValidity;
                row.LastSeenUid = 0;
            }
            row.IsSent = isSent;

            IList<UniqueId> uids;
            if (row.LastSeenUid == 0)
                uids = await folder.SearchAsync(SearchQuery.DeliveredAfter(DateTime.UtcNow - InitialWindow), ct);
            else
                uids = await folder.SearchAsync(SearchQuery.Uids(new UniqueIdRange(new UniqueId((uint)row.LastSeenUid + 1), UniqueId.MaxValue)), ct);
            uids = uids.Where(u => u.Id > row.LastSeenUid).OrderBy(u => u.Id).ToList();

            foreach (var uid in uids)
            {
                ct.ThrowIfCancellationRequested();
                var exists = await db.MailMessages.AnyAsync(m => m.MailFolderId == row.MailFolderId && m.ImapUid == uid.Id, ct);
                if (!exists)
                {
                    MimeMessage mime;
                    try { mime = await folder.GetMessageAsync(uid, ct); }
                    catch { row.LastSeenUid = uid.Id; continue; }
                    var msg = await StoreMessageAsync(db, account, row, uid.Id, mime, isSent, ct);
                    if (msg is not null) synced++;
                }
                row.LastSeenUid = uid.Id;
            }
            await db.SaveChangesAsync(ct);
            await folder.CloseAsync(false, ct);
        }
        await imap.DisconnectAsync(true, ct);
        return synced;
    }

    private static async Task<MailMessage?> StoreMessageAsync(
        OdinDbContext db, MailAccount account, MailFolder folder, long uid, MimeMessage mime, bool outbound, CancellationToken ct)
    {
        static string Join(InternetAddressList list) => string.Join(", ",
            list.Mailboxes.Select(m => m.Address)).TruncateTo(3900);

        var from = mime.From.Mailboxes.FirstOrDefault();
        var msg = new MailMessage
        {
            MailAccountId = account.MailAccountId,
            MailFolderId = folder.MailFolderId,
            ImapUid = uid,
            MessageIdHeader = mime.MessageId?.TruncateTo(480),
            InReplyTo = mime.InReplyTo?.TruncateTo(480),
            Subject = mime.Subject?.TruncateTo(990),
            FromAddress = from?.Address?.TruncateTo(310),
            FromName = from?.Name?.TruncateTo(290),
            ToAddresses = Join(mime.To),
            CcAddresses = Join(mime.Cc),
            SentAt = mime.Date == default ? null : mime.Date.UtcDateTime,
            BodyText = mime.TextBody,
            BodyHtml = mime.HtmlBody,
            IsOutbound = outbound,
            IsRead = outbound,
            SyncedAt = DateTime.UtcNow,
        };
        db.MailMessages.Add(msg);

        foreach (var part in mime.Attachments.OfType<MimePart>())
        {
            var att = new MailAttachment
            {
                MailMessageId = msg.MailMessageId,
                FileName = (part.FileName ?? "attachment").TruncateTo(490),
                ContentType = part.ContentType?.MimeType?.TruncateTo(190),
            };
            using var ms = new MemoryStream();
            await part.Content.DecodeToAsync(ms, ct);
            att.SizeBytes = ms.Length;
            if (ms.Length <= MaxAttachmentBytes) att.Content = ms.ToArray();
            else att.Skipped = true;
            db.MailAttachments.Add(att);
        }

        await LinkMessageAsync(db, msg, ct);
        await db.SaveChangesAsync(ct);
        return msg;
    }

    /// <summary>Link a mail to every matching student / lead / partner by its
    /// counterpart addresses (sender on inbound; recipients on outbound).</summary>
    public static async Task LinkMessageAsync(OdinDbContext db, MailMessage msg, CancellationToken ct)
    {
        var counterparts = new List<string>();
        if (msg.IsOutbound)
        {
            counterparts.AddRange((msg.ToAddresses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            counterparts.AddRange((msg.CcAddresses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else if (!string.IsNullOrWhiteSpace(msg.FromAddress))
        {
            counterparts.Add(msg.FromAddress);
        }
        var addrs = counterparts.Select(a => a.ToLowerInvariant()).Distinct().ToList();
        if (addrs.Count == 0) return;

        var students = await db.Students
            .Where(s => s.DeletedAt == null && s.User.Email != null && addrs.Contains(s.User.Email.ToLower()))
            .Select(s => new { s.StudentId, Email = s.User.Email!.ToLower() })
            .ToListAsync(ct);
        foreach (var s in students)
            db.MailMessageLinks.Add(new MailMessageLink { MailMessageId = msg.MailMessageId, StudentId = s.StudentId, MatchedAddress = s.Email });

        var leads = await db.CrmLeads
            .Where(l => l.DeletedAt == null && l.Email != null && addrs.Contains(l.Email.ToLower()))
            .Select(l => new { l.CrmLeadId, Email = l.Email!.ToLower() })
            .ToListAsync(ct);
        foreach (var l in leads)
            db.MailMessageLinks.Add(new MailMessageLink { MailMessageId = msg.MailMessageId, CrmLeadId = l.CrmLeadId, MatchedAddress = l.Email });

        // Partner match: legacy contact emails ∪ contact-book Email methods ∪
        // partner portal users' login emails.
        var partnerIds = new Dictionary<Guid, string>();
        foreach (var row in await db.PartnerContactEmails
            .Where(e => e.DeletedAt == null && e.Email != null && addrs.Contains(e.Email.ToLower()))
            .Select(e => new { e.PartnerId, Email = e.Email!.ToLower() }).ToListAsync(ct))
            partnerIds.TryAdd(row.PartnerId, row.Email);
        foreach (var row in await db.PartnerContactMethods
            .Where(m => m.MethodType.Name == "Email" && addrs.Contains(m.Value.ToLower()))
            .Select(m => new { m.Contact.PartnerId, Email = m.Value.ToLower() }).ToListAsync(ct))
            partnerIds.TryAdd(row.PartnerId, row.Email);
        foreach (var row in await db.PartnerUsers
            .Where(pu => pu.User.Email != null && addrs.Contains(pu.User.Email.ToLower()))
            .Select(pu => new { pu.PartnerId, Email = pu.User.Email!.ToLower() }).ToListAsync(ct))
            partnerIds.TryAdd(row.PartnerId, row.Email);
        foreach (var (pid, email) in partnerIds)
            db.MailMessageLinks.Add(new MailMessageLink { MailMessageId = msg.MailMessageId, PartnerId = pid, MatchedAddress = email });
    }

    /// <summary>Send via the account's SMTP and store the outbound copy
    /// (linked to its recipients) so hub-sent mail is archived too.</summary>
    public async Task<Guid> SendAsync(
        Guid accountId, string to, string? cc, string subject, string bodyText,
        string? inReplyToHeader, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
        var account = await db.MailAccounts.FirstOrDefaultAsync(a => a.MailAccountId == accountId && a.DeletedAt == null, ct)
            ?? throw new InvalidOperationException("Mail account not found.");
        var password = System.Text.Encoding.UTF8.GetString(FieldEncryption.Decrypt(account.PasswordEncrypted));

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(account.DisplayName, account.EmailAddress));
        foreach (var a in to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            mime.To.Add(MailboxAddress.Parse(a));
        foreach (var a in (cc ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            mime.Cc.Add(MailboxAddress.Parse(a));
        mime.Subject = subject;
        mime.Body = new TextPart("plain") { Text = bodyText };
        if (!string.IsNullOrWhiteSpace(inReplyToHeader))
        {
            mime.InReplyTo = inReplyToHeader;
            mime.References.Add(inReplyToHeader);
        }

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(account.SmtpHost, account.SmtpPort,
            account.SmtpUseSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto, ct);
        await smtp.AuthenticateAsync(account.Username, password, ct);
        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(true, ct);

        var msg = new MailMessage
        {
            MailAccountId = account.MailAccountId,
            MailFolderId = null,
            ImapUid = 0,
            MessageIdHeader = mime.MessageId,
            InReplyTo = inReplyToHeader,
            Subject = subject.TruncateTo(990),
            FromAddress = account.EmailAddress,
            FromName = account.DisplayName,
            ToAddresses = to.TruncateTo(3900),
            CcAddresses = cc?.TruncateTo(3900),
            SentAt = DateTime.UtcNow,
            BodyText = bodyText,
            IsOutbound = true,
            IsRead = true,
            SyncedAt = DateTime.UtcNow,
        };
        db.MailMessages.Add(msg);
        await LinkMessageAsync(db, msg, ct);
        await db.SaveChangesAsync(ct);
        return msg.MailMessageId;
    }
}

internal static class MailStringExtensions
{
    public static string? TruncateTo(this string? s, int max) =>
        s is null ? null : (s.Length <= max ? s : s[..max]);
}

/// <summary>Background auto-sync: every 5 minutes, every enabled account.</summary>
public sealed class MailSyncWorker(MailHubService mail, IServiceScopeFactory scopeFactory, ILogger<MailSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> ids;
                using (var scope = scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
                    ids = await db.MailAccounts
                        .Where(a => a.DeletedAt == null && a.IsEnabled)
                        .Select(a => a.MailAccountId)
                        .ToListAsync(stoppingToken);
                }
                foreach (var id in ids)
                    await mail.SyncAccountAsync(id, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Mail sync sweep failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
