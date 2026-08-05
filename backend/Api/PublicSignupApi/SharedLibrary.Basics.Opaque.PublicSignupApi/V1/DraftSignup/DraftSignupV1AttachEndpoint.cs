using System.Security.Cryptography;
using Odin.Api.Base.Authentication;
using Odin.Api.Base.Email;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Existing-student fast path: when a wizard email matches an already
/// registered student, the flow attaches a NEW application (enrolment under
/// the flow's partner) instead of failing. Staff-run wizards (valid actor
/// ticket) confirm and continue immediately; the public flow must first
/// verify a 6-digit code emailed to the existing address. The wizard then
/// skips the account/personal/background steps and resumes at programme
/// selection with a normal wizard session bound to the existing student.
/// </summary>
[Route("/v1/public/draft-signup/attach-start")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1AttachEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/public/draft-signup/check-existing", CheckAsync).AllowAnonymous();
        app.MapPost("/v1/public/draft-signup/attach-code", SendCodeAsync).AllowAnonymous();
        app.MapPost("/v1/public/draft-signup/attach-start", StartAsync).AllowAnonymous();
        return app;
    }

    public sealed class CheckBody
    {
        public string? PartnerSlug { get; init; }
        public string? Email { get; init; }
        public string? ActorTicket { get; init; }
    }

    public sealed class CodeBody
    {
        public string? PartnerSlug { get; init; }
        public string? Email { get; init; }
    }

    public sealed class StartBody
    {
        public string? PartnerSlug { get; init; }
        public string? Email { get; init; }
        public string? ActorTicket { get; init; }
        public string? AttachCode { get; init; }
        public string? CrmLead { get; init; }
    }

    /// <summary>Attach context read back by /submit — keyed by wizard token.</summary>
    public sealed record AttachState(Guid PartnerId, string? ActorUserId, string? CrmLeadId, bool LinkDocs);
    public static string AttachKey(string wizardToken) => $"wizattach:{wizardToken}";
    private static string CodeKey(string email) => $"wizattachcode:{email.ToLowerInvariant()}";

    private sealed record Found(SharedLibrary.Basics.Opaque.Domains.Student Student, string Email);

    private static async Task<Found?> FindStudentAsync(OdinDbContext db, string? email, CancellationToken ct)
    {
        var e = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(e)) return null;
        var student = await db.Students
            .Where(s => s.DeletedAt == null && s.User.Email != null && s.User.Email.ToLower() == e)
            .OrderByDescending(s => s.WizardStep)
            .FirstOrDefaultAsync(ct);
        return student is null ? null : new Found(student, e);
    }

    private static async Task<(bool LinkDocs, List<Guid> BlockedProgrammeIds)> PartnerContextAsync(
        OdinDbContext db, Guid studentId, Guid partnerId, CancellationToken ct)
    {
        var partnerEnrolments = await db.Enrollments
            .Where(en => en.StudentId == studentId && en.PartnerId == partnerId && en.DeletedAt == null)
            .Select(en => new { en.StudentEnrollmentId, en.Specialization.ProgrammeId })
            .ToListAsync(ct);
        var enrolmentIds = partnerEnrolments.Select(x => x.StudentEnrollmentId).ToList();
        var linkDocs = enrolmentIds.Count > 0 && await db.StudentDocuments.AnyAsync(d =>
            d.EnrollmentId != null && enrolmentIds.Contains(d.EnrollmentId.Value) && d.DeletedAt == null, ct);
        return (linkDocs, partnerEnrolments.Select(x => x.ProgrammeId).Distinct().ToList());
    }

    private static async Task<IResult> CheckAsync(
        [FromBody] CheckBody body, OdinDbContext db, ITransientStateCache cache, CancellationToken ct)
    {
        var found = await FindStudentAsync(db, body.Email, ct);
        if (found is null) return Results.Ok(new { exists = false });

        // Unfinished self-signup drafts keep the existing password-resume path.
        if (found.Student.WizardStep < 6)
            return Results.Ok(new { exists = true, mode = "draft" });

        var partner = await db.Partners.FirstOrDefaultAsync(p => p.Slug == body.PartnerSlug && p.DeletedAt == null, ct);
        if (partner is null) return Results.BadRequest(new { error = "Unknown partner slug." });

        string? actorUserId = null;
        if (!string.IsNullOrWhiteSpace(body.ActorTicket))
            actorUserId = await cache.GetAsync<string>($"wizactor:{body.ActorTicket.Trim()}");
        var staff = actorUserId is not null;

        var (linkDocs, blocked) = await PartnerContextAsync(db, found.Student.StudentId, partner.PartnerId, ct);

        string? name = null;
        if (staff)
        {
            var profile = await db.UserProfiles.Where(p => p.UserId == found.Student.UserId)
                .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
            name = $"{profile?.FirstName} {profile?.LastName}".Trim();
        }
        return Results.Ok(new
        {
            exists = true,
            mode = "attach",
            staff,
            // Identity details only for authenticated staff tickets — the
            // public flow learns nothing until the email code is verified.
            name,
            studentNumber = staff ? found.Student.StudentNumber : null,
            linkDocs = staff ? linkDocs : (bool?)null,
            blockedProgrammeIds = staff ? blocked : null,
        });
    }

    private static async Task<IResult> SendCodeAsync(
        [FromBody] CodeBody body, OdinDbContext db, ITransientStateCache cache,
        IEmailSender emailSender, CancellationToken ct)
    {
        var found = await FindStudentAsync(db, body.Email, ct);
        // Always answer ok so the endpoint can't be used to probe addresses
        // beyond what /check-existing already reveals.
        if (found is null || found.Student.WizardStep < 6) return Results.Ok(new { sent = true });

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        await cache.SetAsync(CodeKey(found.Email), code, TimeSpan.FromMinutes(15));
        var htmlBody =
            "<p>You (or a partner acting for you) started a new programme application with your existing MGW student account.</p>" +
            $"<p>Your confirmation code is:</p><p style=\"font-size:26px;font-weight:800;letter-spacing:4px;\">{code}</p>" +
            "<p style=\"color:#6b7888;font-size:13px;\">The code is valid for 15 minutes. If this wasn't you, ignore this email — nothing happens without the code.</p>";
        await emailSender.SendAsync(new EmailMessage(To: found.Email, Subject: "Your MGW application code", HtmlBody: htmlBody), ct);
        return Results.Ok(new { sent = true });
    }

    private static async Task<IResult> StartAsync(
        [FromBody] StartBody body, OdinDbContext db, ITransientStateCache cache,
        WizardSessionService wizard, CancellationToken ct)
    {
        var found = await FindStudentAsync(db, body.Email, ct);
        if (found is null || found.Student.WizardStep < 6)
            return Results.BadRequest(new { error = "No existing student for this email." });

        var partner = await db.Partners.FirstOrDefaultAsync(p => p.Slug == body.PartnerSlug && p.DeletedAt == null, ct);
        if (partner is null) return Results.BadRequest(new { error = "Unknown partner slug." });

        // Authorise: staff actor ticket OR the emailed 6-digit code.
        string? actorUserId = null;
        if (!string.IsNullOrWhiteSpace(body.ActorTicket))
            actorUserId = await cache.GetAsync<string>($"wizactor:{body.ActorTicket.Trim()}");
        if (actorUserId is null)
        {
            var expected = await cache.GetAsync<string>(CodeKey(found.Email));
            if (expected is null || !string.Equals(expected, body.AttachCode?.Trim(), StringComparison.Ordinal))
                return Results.BadRequest(new { error = "Invalid or expired confirmation code." });
            await cache.RemoveAsync(CodeKey(found.Email));
        }

        var (linkDocs, blocked) = await PartnerContextAsync(db, found.Student.StudentId, partner.PartnerId, ct);

        var token = await wizard.IssueAsync(found.Student.UserId, found.Student.StudentId);
        await cache.SetAsync(AttachKey(token),
            new AttachState(partner.PartnerId, actorUserId,
                Guid.TryParse(body.CrmLead, out var crm) ? crm.ToString() : null, linkDocs),
            TimeSpan.FromHours(6));

        var profile = await db.UserProfiles.Where(p => p.UserId == found.Student.UserId)
            .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
        return Results.Ok(new
        {
            wizardToken = token,
            studentName = $"{profile?.FirstName} {profile?.LastName}".Trim(),
            studentNumber = found.Student.StudentNumber,
            linkDocs,
            blockedProgrammeIds = blocked,
        });
    }
}
