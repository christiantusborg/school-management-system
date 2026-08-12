using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Crypto;
using Odin.Api.Base.Mail;
using SharedLibrary.Basics.Opaque.Domains.Mail;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Admin.V1.Mail;

/// <summary>
/// Webmail hub. Admission users open the accounts they're granted (SuperAdmin
/// opens all and is the only one who may add/edit accounts and access lists),
/// browse folders, read synced mail with links to students / CRM leads /
/// partners, create a lead from an unknown sender, reply / compose via the
/// account's real SMTP, and download stored attachments. Partners and
/// students get read-only lists of mail linked to them.
/// </summary>
[Route("/v1/admin/mail/accounts")]
[EndpointTag("Admin.Mail")]
public sealed class AdminV1MailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/mail/accounts", AccountsAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/mail/accounts", AccountCreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/mail/accounts/{id:guid}", AccountUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/mail/accounts/{id:guid}", AccountDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/mail/accounts/{id:guid}/access", AccountAccessAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/mail/accounts/{id:guid}/sync", SyncAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/mail/messages", MessagesAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/mail/messages/{id:guid}", MessageDetailAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/mail/messages/{id:guid}/read", MarkReadAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/mail/attachments/{id:guid}", AttachmentAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/mail/send", SendAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/mail/messages/{id:guid}/create-lead", CreateLeadAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/leads/{id:guid}/emails", LeadEmailsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/mail/for-student/{studentId:guid}", ForStudentAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/mail/for-partner/{partnerId:guid}", ForPartnerAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/partner/mail", PartnerMailAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/student/me/mail", StudentMailAsync).RequireAuthorization();
        return app;
    }

    // ── access helpers ───────────────────────────────────────────────────────

    private static Guid CallerId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : Guid.Empty;

    private static bool IsSuperAdmin(HttpContext ctx) => ctx.User.IsInRole(AdminLevels.SuperAdministrator);

    private static IQueryable<MailAccount> AccessibleAccounts(OdinDbContext db, HttpContext ctx)
    {
        var q = db.MailAccounts.Where(a => a.DeletedAt == null);
        if (IsSuperAdmin(ctx)) return q;
        var uid = CallerId(ctx);
        return q.Where(a => a.Access.Any(x => x.UserId == uid));
    }

    private static async Task<bool> CanSeeMessageAsync(OdinDbContext db, HttpContext ctx, Guid messageId, CancellationToken ct)
    {
        if (IsSuperAdmin(ctx))
            return await db.MailMessages.AnyAsync(m => m.MailMessageId == messageId, ct);
        var uid = CallerId(ctx);
        return await db.MailMessages.AnyAsync(m => m.MailMessageId == messageId
            && m.Account.Access.Any(x => x.UserId == uid), ct);
    }

    // ── accounts ─────────────────────────────────────────────────────────────

    public sealed class AccountBody
    {
        public string? DisplayName { get; init; }
        public string? EmailAddress { get; init; }
        public string? Color { get; init; }
        public string? ImapHost { get; init; }
        public int? ImapPort { get; init; }
        public bool? ImapUseSsl { get; init; }
        public string? SmtpHost { get; init; }
        public int? SmtpPort { get; init; }
        public bool? SmtpUseSsl { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
        public bool? IsEnabled { get; init; }
    }
    public sealed class AccessBody { public List<Guid>? UserIds { get; init; } }
    public sealed class SendBody
    {
        public Guid AccountId { get; init; }
        public string? To { get; init; }
        public string? Cc { get; init; }
        public string? Subject { get; init; }
        public string? Body { get; init; }
        public Guid? InReplyToMessageId { get; init; }
    }
    public sealed class ReadBody { public bool Read { get; init; } }

    private static async Task<IResult> AccountsAsync(HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var accounts = await AccessibleAccounts(db, ctx)
            .OrderBy(a => a.DisplayName)
            .Select(a => new
            {
                mailAccountId = a.MailAccountId,
                displayName = a.DisplayName,
                emailAddress = a.EmailAddress,
                color = a.Color,
                isEnabled = a.IsEnabled,
                lastSyncAt = a.LastSyncAt,
                lastSyncError = a.LastSyncError,
                imapHost = a.ImapHost, imapPort = a.ImapPort, imapUseSsl = a.ImapUseSsl,
                smtpHost = a.SmtpHost, smtpPort = a.SmtpPort, smtpUseSsl = a.SmtpUseSsl,
                username = a.Username,
                accessUserIds = a.Access.Select(x => x.UserId).ToList(),
                folders = db.MailFolders.Where(f => f.MailAccountId == a.MailAccountId)
                    .OrderBy(f => f.Name)
                    .Select(f => new
                    {
                        mailFolderId = f.MailFolderId,
                        name = f.Name,
                        isSent = f.IsSent,
                        unread = db.MailMessages.Count(m => m.MailFolderId == f.MailFolderId && !m.IsRead),
                    }).ToList(),
            })
            .ToListAsync(ct);
        return Results.Ok(new { items = accounts, isSuperAdmin = IsSuperAdmin(ctx) });
    }

    private static async Task<IResult> AccountCreateAsync(
        [FromBody] AccountBody body, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(ctx.User, AdminPermissions.MailAccountsManage, ct))
            return Results.Json(new { error = "Only SuperAdministrator can add mail accounts." }, statusCode: StatusCodes.Status403Forbidden);
        if (string.IsNullOrWhiteSpace(body.EmailAddress) || string.IsNullOrWhiteSpace(body.ImapHost)
            || string.IsNullOrWhiteSpace(body.SmtpHost) || string.IsNullOrWhiteSpace(body.Username)
            || string.IsNullOrWhiteSpace(body.Password))
            return Results.BadRequest(new { error = "emailAddress, imapHost, smtpHost, username and password are required." });

        var account = new MailAccount
        {
            DisplayName = string.IsNullOrWhiteSpace(body.DisplayName) ? body.EmailAddress.Trim() : body.DisplayName.Trim(),
            EmailAddress = body.EmailAddress.Trim(),
            Color = body.Color?.Trim(),
            ImapHost = body.ImapHost.Trim(),
            ImapPort = body.ImapPort ?? 993,
            ImapUseSsl = body.ImapUseSsl ?? true,
            SmtpHost = body.SmtpHost.Trim(),
            SmtpPort = body.SmtpPort ?? 587,
            SmtpUseSsl = body.SmtpUseSsl ?? true,
            Username = body.Username.Trim(),
            PasswordEncrypted = FieldEncryption.Encrypt(System.Text.Encoding.UTF8.GetBytes(body.Password)),
            IsEnabled = body.IsEnabled ?? true,
        };
        db.MailAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { mailAccountId = account.MailAccountId });
    }

    private static async Task<IResult> AccountUpdateAsync(
        Guid id, [FromBody] AccountBody body, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(ctx.User, AdminPermissions.MailAccountsManage, ct))
            return Results.Json(new { error = "Only SuperAdministrator can edit mail accounts." }, statusCode: StatusCodes.Status403Forbidden);
        var account = await db.MailAccounts.FirstOrDefaultAsync(a => a.MailAccountId == id && a.DeletedAt == null, ct);
        if (account is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.DisplayName)) account.DisplayName = body.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(body.EmailAddress)) account.EmailAddress = body.EmailAddress.Trim();
        if (body.Color is not null) account.Color = body.Color.Trim();
        if (!string.IsNullOrWhiteSpace(body.ImapHost)) account.ImapHost = body.ImapHost.Trim();
        if (body.ImapPort is { } ip) account.ImapPort = ip;
        if (body.ImapUseSsl is { } issl) account.ImapUseSsl = issl;
        if (!string.IsNullOrWhiteSpace(body.SmtpHost)) account.SmtpHost = body.SmtpHost.Trim();
        if (body.SmtpPort is { } sp) account.SmtpPort = sp;
        if (body.SmtpUseSsl is { } sssl) account.SmtpUseSsl = sssl;
        if (!string.IsNullOrWhiteSpace(body.Username)) account.Username = body.Username.Trim();
        if (!string.IsNullOrWhiteSpace(body.Password))
            account.PasswordEncrypted = FieldEncryption.Encrypt(System.Text.Encoding.UTF8.GetBytes(body.Password));
        if (body.IsEnabled is { } en) account.IsEnabled = en;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { mailAccountId = id });
    }

    private static async Task<IResult> AccountDeleteAsync(
        Guid id, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(ctx.User, AdminPermissions.MailAccountsManage, ct))
            return Results.Json(new { error = "Only SuperAdministrator can remove mail accounts." }, statusCode: StatusCodes.Status403Forbidden);
        var account = await db.MailAccounts.FirstOrDefaultAsync(a => a.MailAccountId == id && a.DeletedAt == null, ct);
        if (account is null) return Results.NotFound();
        account.DeletedAt = DateTime.UtcNow;
        account.IsEnabled = false;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { mailAccountId = id });
    }

    private static async Task<IResult> AccountAccessAsync(
        Guid id, [FromBody] AccessBody body, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (!await perms.HasAsync(ctx.User, AdminPermissions.MailAccountsManage, ct))
            return Results.Json(new { error = "Only SuperAdministrator can change mail access." }, statusCode: StatusCodes.Status403Forbidden);
        var exists = await db.MailAccounts.AnyAsync(a => a.MailAccountId == id && a.DeletedAt == null, ct);
        if (!exists) return Results.NotFound();
        var current = await db.MailAccountAccesses.Where(x => x.MailAccountId == id).ToListAsync(ct);
        db.MailAccountAccesses.RemoveRange(current);
        foreach (var uid in (body.UserIds ?? new()).Distinct())
            db.MailAccountAccesses.Add(new MailAccountAccess { MailAccountId = id, UserId = uid });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { mailAccountId = id });
    }

    private static async Task<IResult> SyncAsync(
        Guid id, HttpContext ctx, OdinDbContext db, MailHubService mail, CancellationToken ct)
    {
        var allowed = await AccessibleAccounts(db, ctx).AnyAsync(a => a.MailAccountId == id, ct);
        if (!allowed) return Results.NotFound();
        var (synced, error) = await mail.SyncAccountAsync(id, ct);
        return error is null ? Results.Ok(new { synced }) : Results.BadRequest(new { error });
    }

    // ── messages ─────────────────────────────────────────────────────────────

    private static async Task<IResult> MessagesAsync(
        HttpContext ctx, OdinDbContext db, CancellationToken ct,
        [FromQuery] Guid? accountId = null, [FromQuery] Guid? folderId = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var accessible = AccessibleAccounts(db, ctx).Select(a => a.MailAccountId);
        var q = db.MailMessages.Where(m => accessible.Contains(m.MailAccountId));
        if (accountId is not null) q = q.Where(m => m.MailAccountId == accountId);
        if (folderId is not null) q = q.Where(m => m.MailFolderId == folderId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(m => (m.Subject != null && m.Subject.ToLower().Contains(s))
                || (m.FromAddress != null && m.FromAddress.ToLower().Contains(s))
                || (m.FromName != null && m.FromName.ToLower().Contains(s)));
        }
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(m => m.SentAt ?? m.SyncedAt)
            .Skip(Math.Max(0, page - 1) * Math.Clamp(pageSize, 1, 200))
            .Take(Math.Clamp(pageSize, 1, 200))
            .Select(m => new
            {
                mailMessageId = m.MailMessageId,
                accountId = m.MailAccountId,
                accountName = m.Account.DisplayName,
                accountEmail = m.Account.EmailAddress,
                accountColor = m.Account.Color,
                folderId = m.MailFolderId,
                subject = m.Subject,
                fromAddress = m.FromAddress,
                fromName = m.FromName,
                toAddresses = m.ToAddresses,
                sentAt = m.SentAt ?? m.SyncedAt,
                isOutbound = m.IsOutbound,
                isRead = m.IsRead,
                hasAttachments = m.Attachments.Any(),
                links = m.Links.Select(l => new
                {
                    l.StudentId,
                    studentName = l.StudentId != null
                        ? db.UserProfiles.Where(up => up.UserId == db.Students.Where(st => st.StudentId == l.StudentId).Select(st => st.UserId).FirstOrDefault())
                            .Select(up => (up.FirstName + " " + up.LastName).Trim()).FirstOrDefault()
                        : null,
                    studentNumber = l.StudentId != null
                        ? db.Students.Where(st => st.StudentId == l.StudentId).Select(st => st.StudentNumber).FirstOrDefault()
                        : null,
                    l.CrmLeadId,
                    leadName = l.CrmLeadId != null
                        ? db.CrmLeads.Where(cl => cl.CrmLeadId == l.CrmLeadId).Select(cl => cl.Name).FirstOrDefault()
                        : null,
                    l.PartnerId,
                    partnerName = l.PartnerId != null
                        ? db.Partners.Where(pa => pa.PartnerId == l.PartnerId).Select(pa => pa.Name).FirstOrDefault()
                        : null,
                }).ToList(),
            })
            .ToListAsync(ct);
        return Results.Ok(new { items, total });
    }

    private static async Task<IResult> MessageDetailAsync(
        Guid id, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        if (!await CanSeeMessageAsync(db, ctx, id, ct)) return Results.NotFound();
        var m = await db.MailMessages
            .Where(x => x.MailMessageId == id)
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                accountId = x.MailAccountId,
                accountName = x.Account.DisplayName,
                accountEmail = x.Account.EmailAddress,
                accountColor = x.Account.Color,
                subject = x.Subject,
                fromAddress = x.FromAddress,
                fromName = x.FromName,
                toAddresses = x.ToAddresses,
                ccAddresses = x.CcAddresses,
                sentAt = x.SentAt ?? x.SyncedAt,
                isOutbound = x.IsOutbound,
                bodyHtml = x.BodyHtml,
                bodyText = x.BodyText,
                messageIdHeader = x.MessageIdHeader,
                attachments = x.Attachments.Select(a => new
                {
                    mailAttachmentId = a.MailAttachmentId,
                    fileName = a.FileName,
                    contentType = a.ContentType,
                    sizeBytes = a.SizeBytes,
                    skipped = a.Skipped,
                }).ToList(),
                links = x.Links.Select(l => new
                {
                    l.StudentId,
                    studentNumber = l.StudentId != null
                        ? db.Students.Where(s => s.StudentId == l.StudentId).Select(s => s.StudentNumber).FirstOrDefault() : null,
                    l.CrmLeadId,
                    leadName = l.CrmLeadId != null
                        ? db.CrmLeads.Where(cl => cl.CrmLeadId == l.CrmLeadId).Select(cl => cl.Name).FirstOrDefault() : null,
                    l.PartnerId,
                    partnerName = l.PartnerId != null
                        ? db.Partners.Where(p => p.PartnerId == l.PartnerId).Select(p => p.Name).FirstOrDefault() : null,
                }).ToList(),
            })
            .FirstOrDefaultAsync(ct);
        if (m is null) return Results.NotFound();

        // Opening a mail marks it read (hub-local flag only).
        await db.MailMessages.Where(x => x.MailMessageId == id && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);
        return Results.Ok(m);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id, [FromBody] ReadBody body, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        if (!await CanSeeMessageAsync(db, ctx, id, ct)) return Results.NotFound();
        await db.MailMessages.Where(x => x.MailMessageId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, body.Read), ct);
        return Results.Ok(new { mailMessageId = id, read = body.Read });
    }

    private static async Task<IResult> AttachmentAsync(
        Guid id, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var att = await db.MailAttachments
            .Where(a => a.MailAttachmentId == id)
            .Select(a => new { a.MailMessageId, a.FileName, a.ContentType, a.Content })
            .FirstOrDefaultAsync(ct);
        if (att is null || att.Content is null) return Results.NotFound();
        if (!await CanSeeMessageAsync(db, ctx, att.MailMessageId, ct)) return Results.NotFound();
        return Results.File(att.Content, att.ContentType ?? "application/octet-stream", att.FileName);
    }

    private static async Task<IResult> SendAsync(
        [FromBody] SendBody body, HttpContext ctx, OdinDbContext db, MailHubService mail, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.To) || string.IsNullOrWhiteSpace(body.Body))
            return Results.BadRequest(new { error = "to and body are required." });
        var allowed = await AccessibleAccounts(db, ctx).AnyAsync(a => a.MailAccountId == body.AccountId, ct);
        if (!allowed) return Results.NotFound(new { error = "Mail account not found or not accessible." });

        string? inReplyTo = null;
        if (body.InReplyToMessageId is { } rid)
            inReplyTo = await db.MailMessages.Where(m => m.MailMessageId == rid)
                .Select(m => m.MessageIdHeader).FirstOrDefaultAsync(ct);
        try
        {
            var msgId = await mail.SendAsync(body.AccountId, body.To.Trim(), body.Cc?.Trim(),
                body.Subject?.Trim() ?? "", body.Body, inReplyTo, ct);
            return Results.Ok(new { mailMessageId = msgId });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Send failed: {ex.Message}" });
        }
    }

    /// <summary>Create a CRM lead from an unknown sender and link the mail.</summary>
    private static async Task<IResult> CreateLeadAsync(
        Guid id, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        if (!await CanSeeMessageAsync(db, ctx, id, ct)) return Results.NotFound();
        var m = await db.MailMessages.FirstOrDefaultAsync(x => x.MailMessageId == id, ct);
        if (m is null || string.IsNullOrWhiteSpace(m.FromAddress)) return Results.BadRequest(new { error = "No sender address on this mail." });

        var existing = await db.CrmLeads.FirstOrDefaultAsync(l =>
            l.DeletedAt == null && l.Email != null && l.Email.ToLower() == m.FromAddress.ToLower(), ct);
        if (existing is not null)
        {
            if (!await db.MailMessageLinks.AnyAsync(x => x.MailMessageId == id && x.CrmLeadId == existing.CrmLeadId, ct))
                db.MailMessageLinks.Add(new MailMessageLink { MailMessageId = id, CrmLeadId = existing.CrmLeadId, MatchedAddress = m.FromAddress.ToLower() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { crmLeadId = existing.CrmLeadId, existed = true });
        }

        var pipeline = await db.CrmPipelines.Where(p => p.DeletedAt == null)
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.DisplayOrder)
            .FirstOrDefaultAsync(ct);
        if (pipeline is null) return Results.BadRequest(new { error = "No CRM pipeline configured." });
        var firstStage = await db.CrmStages
            .Where(s => s.CrmPipelineId == pipeline.CrmPipelineId && s.DeletedAt == null && s.StageType == 0)
            .OrderBy(s => s.DisplayOrder).FirstOrDefaultAsync(ct);
        if (firstStage is null) return Results.BadRequest(new { error = "Pipeline has no open stage." });

        var callerId = CallerId(ctx);
        var isSales = ctx.User.IsInRole("Sales");
        var lead = new SharedLibrary.Basics.Opaque.Domains.Crm.CrmLead
        {
            CrmPipelineId = pipeline.CrmPipelineId,
            CrmStageId = firstStage.CrmStageId,
            Name = string.IsNullOrWhiteSpace(m.FromName) ? m.FromAddress : m.FromName!,
            Email = m.FromAddress,
            Note = $"Created from email: {m.Subject}",
            CreatedByUserId = callerId,
            AssignedToUserId = isSales ? callerId : null,
            StageEnteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.CrmLeads.Add(lead);
        // Link the WHOLE history with this sender, not only the open mail.
        await MailHubService.RetroLinkLeadAsync(db, lead.CrmLeadId, m.FromAddress, ct);
        db.CrmActivities.Add(new SharedLibrary.Basics.Opaque.Domains.Crm.CrmActivity
        {
            CrmLeadId = lead.CrmLeadId,
            Kind = 6,
            Body = $"Lead created from email \"{m.Subject}\"",
            OccurredAt = DateTime.UtcNow,
            ActorUserId = callerId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = lead.CrmLeadId, existed = false });
    }

    // ── linked views: CRM lead / partner portal / student portal ─────────────

    private static async Task<IResult> LeadEmailsAsync(
        Guid id, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        // Same visibility rule as CRM leads: Sales sees own/assigned.
        var callerId = CallerId(ctx);
        var isSales = ctx.User.IsInRole("Sales");
        var visible = await db.CrmLeads.AnyAsync(l => l.CrmLeadId == id && l.DeletedAt == null
            && (!isSales || l.CreatedByUserId == callerId || l.AssignedToUserId == callerId), ct);
        if (!visible) return Results.NotFound();

        var items = await db.MailMessageLinks
            .Where(x => x.CrmLeadId == id)
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                subject = x.Message.Subject,
                fromAddress = x.Message.FromAddress,
                toAddresses = x.Message.ToAddresses,
                sentAt = x.Message.SentAt ?? x.Message.SyncedAt,
                isOutbound = x.Message.IsOutbound,
                accountName = x.Message.Account.DisplayName,
                accountColor = x.Message.Account.Color,
                snippet = x.Message.BodyText != null
                    ? x.Message.BodyText.Substring(0, Math.Min(200, x.Message.BodyText.Length)) : null,
            })
            .OrderByDescending(x => x.sentAt)
            .Take(100)
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    /// <summary>Entity mail log for the admin drawer: every mail linked to
    /// the student/partner from accounts the caller may open, plus the
    /// default To address for composing from the same panel.</summary>
    private static async Task<IResult> ForStudentAsync(
        Guid studentId, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var accessible = AccessibleAccounts(db, ctx).Select(a => a.MailAccountId);
        var items = await db.MailMessageLinks
            .Where(x => x.StudentId == studentId && accessible.Contains(x.Message.MailAccountId))
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                subject = x.Message.Subject,
                fromAddress = x.Message.FromAddress,
                fromName = x.Message.FromName,
                toAddresses = x.Message.ToAddresses,
                sentAt = x.Message.SentAt ?? x.Message.SyncedAt,
                isOutbound = x.Message.IsOutbound,
                accountName = x.Message.Account.DisplayName,
                accountColor = x.Message.Account.Color,
                bodyText = x.Message.BodyText,
            })
            .OrderByDescending(x => x.sentAt)
            .Take(200)
            .ToListAsync(ct);
        var email = await db.Students.Where(st => st.StudentId == studentId)
            .Select(st => st.User.Email).FirstOrDefaultAsync(ct);
        return Results.Ok(new { items, defaultTo = email });
    }

    private static async Task<IResult> ForPartnerAsync(
        Guid partnerId, HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var accessible = AccessibleAccounts(db, ctx).Select(a => a.MailAccountId);
        var items = await db.MailMessageLinks
            .Where(x => x.PartnerId == partnerId && accessible.Contains(x.Message.MailAccountId))
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                subject = x.Message.Subject,
                fromAddress = x.Message.FromAddress,
                fromName = x.Message.FromName,
                toAddresses = x.Message.ToAddresses,
                sentAt = x.Message.SentAt ?? x.Message.SyncedAt,
                isOutbound = x.Message.IsOutbound,
                accountName = x.Message.Account.DisplayName,
                accountColor = x.Message.Account.Color,
                bodyText = x.Message.BodyText,
            })
            .OrderByDescending(x => x.sentAt)
            .Take(200)
            .ToListAsync(ct);
        var email = await db.PartnerContactEmails
            .Where(e => e.PartnerId == partnerId && e.DeletedAt == null && e.Email != null)
            .OrderByDescending(e => e.IsPrimary)
            .Select(e => e.Email)
            .FirstOrDefaultAsync(ct)
            ?? await db.PartnerContactMethods
                .Where(m => m.Contact.PartnerId == partnerId && m.MethodType.Name == "Email")
                .Select(m => m.Value)
                .FirstOrDefaultAsync(ct);
        return Results.Ok(new { items, defaultTo = email });
    }

    private static async Task<IResult> PartnerMailAsync(
        HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(ctx, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        var items = await db.MailMessageLinks
            .Where(x => x.PartnerId == partnerId)
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                subject = x.Message.Subject,
                fromAddress = x.Message.FromAddress,
                fromName = x.Message.FromName,
                toAddresses = x.Message.ToAddresses,
                sentAt = x.Message.SentAt ?? x.Message.SyncedAt,
                isOutbound = x.Message.IsOutbound,
                accountName = x.Message.Account.DisplayName,
                bodyText = x.Message.BodyText,
            })
            .OrderByDescending(x => x.sentAt)
            .Take(300)
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> StudentMailAsync(
        HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Results.Unauthorized();
        var studentId = await db.Students.Where(s => s.UserId == userId && s.DeletedAt == null)
            .Select(s => (Guid?)s.StudentId).FirstOrDefaultAsync(ct);
        if (studentId is null) return Results.NotFound();
        var items = await db.MailMessageLinks
            .Where(x => x.StudentId == studentId)
            .Select(x => new
            {
                mailMessageId = x.MailMessageId,
                subject = x.Message.Subject,
                fromAddress = x.Message.FromAddress,
                fromName = x.Message.FromName,
                sentAt = x.Message.SentAt ?? x.Message.SyncedAt,
                isOutbound = x.Message.IsOutbound,
                accountName = x.Message.Account.DisplayName,
                bodyText = x.Message.BodyText,
            })
            .OrderByDescending(x => x.sentAt)
            .Take(300)
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }
}
