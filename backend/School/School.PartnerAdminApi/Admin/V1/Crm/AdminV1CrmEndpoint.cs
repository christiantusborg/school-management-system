using System.Security.Claims;
using SharedLibrary.Basics.Opaque.Domains.Crm;

namespace School.PartnerAdminApi.Admin.V1.Crm;

/// <summary>
/// IBSS CRM — concepts adapted from the QuVian core CRM, reimplemented for
/// the Sales portal: multi-pipeline lead kanban with per-stage SLA rotting,
/// deterministic scoring, activity timeline with due follow-ups (next
/// actions), My Day attention buckets, round-robin assignment, CSV import
/// with duplicate detection, source ROI report and convert-to-student via
/// the prefilled signup wizard. Sales users see leads they created or are
/// assigned; every admission level sees all leads.
/// </summary>
[Route("/v1/admin/crm/board")]
[EndpointTag("Admin.Crm")]
public sealed class AdminV1CrmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/crm/board", BoardAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/leads/{id:guid}", DetailAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/crm/leads", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/crm/leads/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/crm/leads/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/crm/leads/{id:guid}/stage", SetStageAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/crm/leads/{id:guid}/assign", AssignAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/leads/check-email", CheckEmailAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/crm/leads/{id:guid}/activities", AddActivityAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/crm/activities/{id:guid}/status", SetActivityStatusAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/crm/activities/{id:guid}", DeleteActivityAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/next-actions", NextActionsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/my-day", MyDayAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/source-report", SourceReportAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/crm/import", ImportAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/crm/options", OptionsAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    // ── scope ────────────────────────────────────────────────────────────────

    private sealed record Scope(Guid UserId, bool IsSales);

    private static Scope ResolveScope(HttpContext httpContext)
    {
        var userId = Guid.TryParse(
            httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : Guid.Empty;
        return new Scope(userId, httpContext.User.IsInRole("Sales"));
    }

    private static IQueryable<CrmLead> Scoped(OdinDbContext db, Scope scope) =>
        scope.IsSales
            ? db.CrmLeads.Where(l => l.DeletedAt == null
                && (l.CreatedByUserId == scope.UserId || l.AssignedToUserId == scope.UserId))
            : db.CrmLeads.Where(l => l.DeletedAt == null);

    // ── scoring ──────────────────────────────────────────────────────────────

    /// <summary>Deterministic score from structural signals; ≥ 70 = hot.</summary>
    internal static int ComputeScore(CrmLead lead, bool recentActivity) =>
        (lead.ValueBand switch { 2 => 40, 1 => 25, 0 => 10, _ => 0 })
        + (lead.CrmLeadSourceId is not null ? 15 : 0)
        + (lead.PartnerId is not null ? 15 : 0)
        + (lead.ProgrammeId is not null ? 15 : 0)
        + (recentActivity ? 15 : 0);

    private static async Task RescoreAsync(OdinDbContext db, CrmLead lead, CancellationToken ct)
    {
        var recent = await db.CrmActivities.AnyAsync(a =>
            a.CrmLeadId == lead.CrmLeadId && a.Kind != 6
            && a.OccurredAt >= DateTime.UtcNow.AddDays(-7), ct);
        lead.Score = ComputeScore(lead, recent);
    }

    // ── dtos ─────────────────────────────────────────────────────────────────

    public sealed class LeadBody
    {
        public Guid? PipelineId { get; init; }
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Country { get; init; }
        public string? Note { get; init; }
        public Guid? SourceId { get; init; }
        public Guid? PartnerId { get; init; }
        public Guid? ProgrammeId { get; init; }
        public int? ValueBand { get; init; }
        public Guid? AssignedToUserId { get; init; }
    }

    public sealed class StageBody { public Guid StageId { get; init; } public string? LostReason { get; init; } }
    public sealed class AssignBody { public Guid? UserId { get; init; } }
    public sealed class ActivityBody
    {
        public int Kind { get; init; }
        public string? Body { get; init; }
        public DateTime? OccurredAt { get; init; }
        public DateTime? DueAt { get; init; }
    }
    public sealed class ActivityStatusBody { public bool Completed { get; init; } }
    public sealed class ImportRow { public string? Name { get; init; } public string? Email { get; init; } public string? Phone { get; init; } public string? Country { get; init; } }
    public sealed class ImportBody
    {
        public Guid PipelineId { get; init; }
        public Guid? SourceId { get; init; }
        public bool Commit { get; init; }
        public List<ImportRow>? Rows { get; init; }
    }

    private static object LeadCard(CrmLead l, Dictionary<Guid, string> userNames, DateTime now) => new
    {
        crmLeadId = l.CrmLeadId,
        name = l.Name,
        email = l.Email,
        phone = l.Phone,
        country = l.Country,
        stageId = l.CrmStageId,
        pipelineId = l.CrmPipelineId,
        status = l.Status,
        valueBand = l.ValueBand,
        score = l.Score,
        hot = l.Score >= 70,
        sourceId = l.CrmLeadSourceId,
        partnerId = l.PartnerId,
        programmeId = l.ProgrammeId,
        assignedToUserId = l.AssignedToUserId,
        assignedToName = l.AssignedToUserId is { } au && userNames.TryGetValue(au, out var an) ? an : null,
        createdByUserId = l.CreatedByUserId,
        createdByName = userNames.TryGetValue(l.CreatedByUserId, out var cn) ? cn : null,
        stageEnteredAt = l.StageEnteredAt,
        createdAt = l.CreatedAt,
        convertedStudentId = l.ConvertedStudentId,
        lostReason = l.LostReason,
        note = l.Note,
        rotting = l.Status == 0 && l.Stage.SlaHours is { } sla && l.StageEnteredAt.AddHours(sla) < now,
        nextDueAt = l.Activities
            .Where(a => a.DueAt != null && a.CompletedAt == null)
            .OrderBy(a => a.DueAt).Select(a => a.DueAt).FirstOrDefault(),
    };

    private static async Task<Dictionary<Guid, string>> UserNamesAsync(OdinDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var strIds = ids.Distinct().Select(x => x.ToString()).ToList();
        var rows = await db.UserProfiles
            .Where(p => strIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.FirstName, p.LastName })
            .ToListAsync(ct);
        return rows
            .Where(r => Guid.TryParse(r.UserId, out _))
            .ToDictionary(r => Guid.Parse(r.UserId),
                r => $"{r.FirstName} {r.LastName}".Trim());
    }

    // ── board ────────────────────────────────────────────────────────────────

    private static async Task<IResult> BoardAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct, [FromQuery] Guid? pipelineId = null)
    {
        var scope = ResolveScope(httpContext);
        var pipelines = await db.CrmPipelines.Where(p => p.DeletedAt == null)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new { crmPipelineId = p.CrmPipelineId, name = p.Name, isDefault = p.IsDefault })
            .ToListAsync(ct);
        var active = pipelineId ?? pipelines.FirstOrDefault(p => p.isDefault)?.crmPipelineId ?? pipelines.FirstOrDefault()?.crmPipelineId;

        var stages = await db.CrmStages
            .Where(s => s.CrmPipelineId == active && s.DeletedAt == null)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new { crmStageId = s.CrmStageId, name = s.Name, color = s.Color, stageType = s.StageType, slaHours = s.SlaHours, displayOrder = s.DisplayOrder })
            .ToListAsync(ct);

        var leads = await Scoped(db, scope)
            .Where(l => l.CrmPipelineId == active)
            .Include(l => l.Stage)
            .Include(l => l.Activities)
            .OrderByDescending(l => l.Score).ThenBy(l => l.CreatedAt)
            .ToListAsync(ct);

        var names = await UserNamesAsync(db,
            leads.SelectMany(l => new[] { l.CreatedByUserId, l.AssignedToUserId ?? Guid.Empty }).Where(x => x != Guid.Empty), ct);
        var now = DateTime.UtcNow;
        return Results.Ok(new
        {
            pipelines,
            activePipelineId = active,
            stages,
            leads = leads.Select(l => LeadCard(l, names, now)),
        });
    }

    private static async Task<IResult> DetailAsync(
        Guid id, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope)
            .Include(l => l.Stage).Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();

        var actorIds = lead.Activities.Select(a => a.ActorUserId)
            .Concat(new[] { lead.CreatedByUserId, lead.AssignedToUserId ?? Guid.Empty })
            .Where(x => x != Guid.Empty);
        var names = await UserNamesAsync(db, actorIds, ct);

        string? convertedStudentNumber = null;
        if (lead.ConvertedStudentId is { } sid)
            convertedStudentNumber = await db.Students.Where(s => s.StudentId == sid)
                .Select(s => s.StudentNumber).FirstOrDefaultAsync(ct);

        var card = LeadCard(lead, names, DateTime.UtcNow);
        return Results.Ok(new
        {
            lead = card,
            convertedStudentNumber,
            activities = lead.Activities.OrderByDescending(a => a.OccurredAt).Select(a => new
            {
                crmActivityId = a.CrmActivityId,
                kind = a.Kind,
                body = a.Body,
                occurredAt = a.OccurredAt,
                dueAt = a.DueAt,
                completedAt = a.CompletedAt,
                actorName = names.TryGetValue(a.ActorUserId, out var n) ? n : null,
            }),
        });
    }

    // ── lead CRUD ────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateAsync(
        [FromBody] LeadBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });

        var pipelineId = body.PipelineId
            ?? await db.CrmPipelines.Where(p => p.DeletedAt == null && p.IsDefault).Select(p => (Guid?)p.CrmPipelineId).FirstOrDefaultAsync(ct)
            ?? await db.CrmPipelines.Where(p => p.DeletedAt == null).OrderBy(p => p.DisplayOrder).Select(p => (Guid?)p.CrmPipelineId).FirstOrDefaultAsync(ct);
        if (pipelineId is null) return Results.BadRequest(new { error = "No pipeline configured." });

        var firstStage = await db.CrmStages
            .Where(s => s.CrmPipelineId == pipelineId && s.DeletedAt == null && s.StageType == 0)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(ct);
        if (firstStage is null) return Results.BadRequest(new { error = "Pipeline has no open stage." });

        var lead = new CrmLead
        {
            CrmPipelineId = pipelineId.Value,
            CrmStageId = firstStage.CrmStageId,
            Status = 0,
            Name = name,
            Email = body.Email?.Trim(),
            Phone = body.Phone?.Trim(),
            Country = body.Country?.Trim(),
            Note = body.Note?.Trim(),
            CrmLeadSourceId = body.SourceId,
            PartnerId = body.PartnerId,
            ProgrammeId = body.ProgrammeId,
            ValueBand = body.ValueBand,
            AssignedToUserId = body.AssignedToUserId,
            CreatedByUserId = scope.UserId,
            StageEnteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        // Sales users own what they create by default.
        if (lead.AssignedToUserId is null && scope.IsSales) lead.AssignedToUserId = scope.UserId;
        // Round-robin fallback for unassigned leads when the pool is on.
        if (lead.AssignedToUserId is null) lead.AssignedToUserId = await NextRoundRobinAsync(db, ct);
        lead.Score = ComputeScore(lead, recentActivity: false);
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = lead.CrmLeadId });
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, [FromBody] LeadBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope).FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name)) lead.Name = body.Name.Trim();
        lead.Email = body.Email?.Trim();
        lead.Phone = body.Phone?.Trim();
        lead.Country = body.Country?.Trim();
        lead.Note = body.Note?.Trim();
        lead.CrmLeadSourceId = body.SourceId;
        lead.PartnerId = body.PartnerId;
        lead.ProgrammeId = body.ProgrammeId;
        lead.ValueBand = body.ValueBand;
        await RescoreAsync(db, lead, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = id });
    }

    private static async Task<IResult> DeleteAsync(
        Guid id, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope).FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();
        lead.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = id });
    }

    private static async Task<IResult> SetStageAsync(
        Guid id, [FromBody] StageBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope).FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();
        var stage = await db.CrmStages.FirstOrDefaultAsync(s =>
            s.CrmStageId == body.StageId && s.CrmPipelineId == lead.CrmPipelineId && s.DeletedAt == null, ct);
        if (stage is null) return Results.BadRequest(new { error = "Stage not found in this pipeline." });

        if (lead.CrmStageId != stage.CrmStageId)
        {
            lead.CrmStageId = stage.CrmStageId;
            lead.StageEnteredAt = DateTime.UtcNow;
            lead.Status = stage.StageType switch { 1 => 1, 2 => 2, _ => 0 };
            if (stage.StageType == 2) lead.LostReason = body.LostReason?.Trim();
            db.CrmActivities.Add(new CrmActivity
            {
                CrmLeadId = lead.CrmLeadId,
                Kind = 6,
                Body = $"Moved to stage: {stage.Name}",
                OccurredAt = DateTime.UtcNow,
                ActorUserId = scope.UserId,
                CreatedAt = DateTime.UtcNow,
            });
        }
        await RescoreAsync(db, lead, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = id, stageId = stage.CrmStageId, status = lead.Status });
    }

    private static async Task<Guid?> NextRoundRobinAsync(OdinDbContext db, CancellationToken ct)
    {
        var cfg = await db.CrmAssignmentConfigs.FirstOrDefaultAsync(c => c.CrmAssignmentConfigId == 1, ct);
        if (cfg is null || cfg.Strategy != 1 || string.IsNullOrWhiteSpace(cfg.MemberUserIds)) return null;
        var pool = cfg.MemberUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty).Where(x => x != Guid.Empty).ToArray();
        if (pool.Length == 0) return null;
        cfg.LastAssignedIndex = (cfg.LastAssignedIndex + 1) % pool.Length;
        return pool[cfg.LastAssignedIndex];
    }

    private static async Task<IResult> AssignAsync(
        Guid id, [FromBody] AssignBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope).FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();
        // Explicit pick wins; null = draw from the round-robin pool.
        lead.AssignedToUserId = body.UserId ?? await NextRoundRobinAsync(db, ct) ?? lead.AssignedToUserId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadId = id, assignedToUserId = lead.AssignedToUserId });
    }

    private static async Task<IResult> CheckEmailAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct, [FromQuery] string? email = null)
    {
        var e = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(e)) return Results.Ok(new { leads = Array.Empty<object>(), students = Array.Empty<object>() });
        var leads = await db.CrmLeads
            .Where(l => l.DeletedAt == null && l.Email != null && l.Email.ToLower() == e)
            .Select(l => new { crmLeadId = l.CrmLeadId, name = l.Name, status = l.Status })
            .Take(5).ToListAsync(ct);
        var students = await db.Students
            .Where(s => s.DeletedAt == null && s.User.Email != null && s.User.Email.ToLower() == e)
            .Select(s => new { studentId = s.StudentId, studentNumber = s.StudentNumber })
            .Take(5).ToListAsync(ct);
        return Results.Ok(new { leads, students });
    }

    // ── activities ───────────────────────────────────────────────────────────

    private static async Task<IResult> AddActivityAsync(
        Guid id, [FromBody] ActivityBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var lead = await Scoped(db, scope).FirstOrDefaultAsync(l => l.CrmLeadId == id, ct);
        if (lead is null) return Results.NotFound();
        if (body.Kind is < 0 or > 5) return Results.BadRequest(new { error = "Invalid activity kind." });
        var act = new CrmActivity
        {
            CrmLeadId = id,
            Kind = body.Kind,
            Body = body.Body?.Trim(),
            OccurredAt = body.OccurredAt ?? DateTime.UtcNow,
            DueAt = body.DueAt,
            ActorUserId = scope.UserId,
            CreatedAt = DateTime.UtcNow,
        };
        db.CrmActivities.Add(act);
        await RescoreAsync(db, lead, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmActivityId = act.CrmActivityId });
    }

    private static async Task<IResult> SetActivityStatusAsync(
        Guid id, [FromBody] ActivityStatusBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var act = await db.CrmActivities.Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.CrmActivityId == id, ct);
        if (act is null || act.Lead.DeletedAt != null) return Results.NotFound();
        if (scope.IsSales && act.Lead.CreatedByUserId != scope.UserId && act.Lead.AssignedToUserId != scope.UserId)
            return Results.NotFound();
        act.CompletedAt = body.Completed ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmActivityId = id, completedAt = act.CompletedAt });
    }

    private static async Task<IResult> DeleteActivityAsync(
        Guid id, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var act = await db.CrmActivities.Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.CrmActivityId == id, ct);
        if (act is null) return Results.NotFound();
        if (scope.IsSales && act.Lead.CreatedByUserId != scope.UserId && act.Lead.AssignedToUserId != scope.UserId)
            return Results.NotFound();
        db.CrmActivities.Remove(act);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmActivityId = id });
    }

    // ── queues & dashboards ──────────────────────────────────────────────────

    private static async Task<IResult> NextActionsAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var leadIds = Scoped(db, scope).Where(l => l.Status == 0).Select(l => l.CrmLeadId);
        var items = await db.CrmActivities
            .Where(a => leadIds.Contains(a.CrmLeadId) && a.DueAt != null && a.CompletedAt == null)
            .OrderBy(a => a.DueAt)
            .Select(a => new
            {
                crmActivityId = a.CrmActivityId,
                crmLeadId = a.CrmLeadId,
                leadName = a.Lead.Name,
                kind = a.Kind,
                body = a.Body,
                dueAt = a.DueAt,
                overdue = a.DueAt < DateTime.UtcNow,
            })
            .Take(200)
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> MyDayAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var now = DateTime.UtcNow;
        var open = await Scoped(db, scope)
            .Where(l => l.Status == 0)
            .Include(l => l.Stage)
            .Include(l => l.Activities)
            .ToListAsync(ct);

        List<object> Bucket(IEnumerable<CrmLead> ls) => ls
            .OrderByDescending(l => l.Score)
            .Take(25)
            .Select(l => (object)new { crmLeadId = l.CrmLeadId, name = l.Name, score = l.Score, stageName = l.Stage.Name })
            .ToList();

        bool HasOpenDue(CrmLead l) => l.Activities.Any(a => a.DueAt != null && a.CompletedAt == null);
        bool Overdue(CrmLead l) => l.Activities.Any(a => a.DueAt != null && a.CompletedAt == null && a.DueAt < now);
        bool Rotting(CrmLead l) => l.Stage.SlaHours is { } sla && l.StageEnteredAt.AddHours(sla) < now;
        bool Stale(CrmLead l) => l.StageEnteredAt < now.AddDays(-14);

        return Results.Ok(new
        {
            counts = new
            {
                open = open.Count,
                overdue = open.Count(Overdue),
                noNextAction = open.Count(l => !HasOpenDue(l)),
                rotting = open.Count(Rotting),
                stale = open.Count(Stale),
                hot = open.Count(l => l.Score >= 70),
                unassigned = open.Count(l => l.AssignedToUserId == null),
            },
            overdue = Bucket(open.Where(Overdue)),
            noNextAction = Bucket(open.Where(l => !HasOpenDue(l))),
            rotting = Bucket(open.Where(Rotting)),
            stale = Bucket(open.Where(Stale)),
            hot = Bucket(open.Where(l => l.Score >= 70)),
            unassigned = Bucket(open.Where(l => l.AssignedToUserId == null)),
        });
    }

    private static async Task<IResult> SourceReportAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var rows = await Scoped(db, scope)
            .GroupBy(l => l.CrmLeadSourceId)
            .Select(g => new { sourceId = g.Key, total = g.Count(),
                converted = g.Count(l => l.Status == 1), lost = g.Count(l => l.Status == 2) })
            .ToListAsync(ct);
        var sources = await db.CrmLeadSources.Where(s => s.DeletedAt == null)
            .Select(s => new { s.CrmLeadSourceId, s.Name }).ToListAsync(ct);
        var items = rows.Select(r => new
        {
            r.sourceId,
            sourceName = sources.FirstOrDefault(s => s.CrmLeadSourceId == r.sourceId)?.Name ?? "(no source)",
            r.total,
            r.converted,
            r.lost,
            open = r.total - r.converted - r.lost,
            conversionPct = r.total == 0 ? 0 : Math.Round(100.0 * r.converted / r.total, 1),
            // Win rate = converted of DECIDED (converted + lost) — the core
            // CRM's nuance: don't punish sources whose leads are still open.
            winRatePct = (r.converted + r.lost) == 0 ? 0 : Math.Round(100.0 * r.converted / (r.converted + r.lost), 1),
        }).OrderByDescending(x => x.total).ToList();
        return Results.Ok(new { items });
    }

    // ── import ───────────────────────────────────────────────────────────────

    private static async Task<IResult> ImportAsync(
        [FromBody] ImportBody body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var scope = ResolveScope(httpContext);
        var rows = (body.Rows ?? new()).Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        var noName = (body.Rows?.Count ?? 0) - rows.Count;

        var emails = rows.Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .Select(r => r.Email!.Trim().ToLowerInvariant()).ToList();
        var existingLeadEmails = await db.CrmLeads
            .Where(l => l.DeletedAt == null && l.Email != null && emails.Contains(l.Email.ToLower()))
            .Select(l => l.Email!.ToLower()).ToListAsync(ct);
        var existingStudentEmails = await db.Students
            .Where(s => s.DeletedAt == null && s.User.Email != null && emails.Contains(s.User.Email.ToLower()))
            .Select(s => s.User.Email!.ToLower()).ToListAsync(ct);
        var dupes = existingLeadEmails.Concat(existingStudentEmails).ToHashSet();

        // Dedup inside the file too: first-seen email wins.
        var seen = new HashSet<string>();
        var importable = new List<ImportRow>();
        var dupCount = 0;
        foreach (var r in rows)
        {
            var e = r.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(e) && (dupes.Contains(e) || !seen.Add(e))) { dupCount++; continue; }
            importable.Add(r);
        }

        if (!body.Commit)
            return Results.Ok(new { plan = true, importable = importable.Count, duplicates = dupCount, noName });

        var firstStage = await db.CrmStages
            .Where(s => s.CrmPipelineId == body.PipelineId && s.DeletedAt == null && s.StageType == 0)
            .OrderBy(s => s.DisplayOrder).FirstOrDefaultAsync(ct);
        if (firstStage is null) return Results.BadRequest(new { error = "Pipeline has no open stage." });

        foreach (var r in importable)
        {
            var lead = new CrmLead
            {
                CrmPipelineId = body.PipelineId,
                CrmStageId = firstStage.CrmStageId,
                Name = r.Name!.Trim(),
                Email = r.Email?.Trim(),
                Phone = r.Phone?.Trim(),
                Country = r.Country?.Trim(),
                CrmLeadSourceId = body.SourceId,
                CreatedByUserId = scope.UserId,
                AssignedToUserId = scope.IsSales ? scope.UserId : await NextRoundRobinAsync(db, ct),
                StageEnteredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            lead.Score = ComputeScore(lead, recentActivity: false);
            db.CrmLeads.Add(lead);
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { plan = false, imported = importable.Count, duplicates = dupCount, noName });
    }

    // ── options (partners / programmes / sales users for the lead form) ──────

    private static async Task<IResult> OptionsAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var partners = await db.Partners.Where(p => p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .Select(p => new { partnerId = p.PartnerId, name = p.Name, slug = p.Slug })
            .ToListAsync(ct);
        var programmes = await db.Programmes.Where(p => p.DeletedAt == null && p.OwnerId == null)
            .OrderBy(p => p.Name)
            .Select(p => new { programmeId = p.ProgrammeId, name = p.Name, code = p.Code })
            .ToListAsync(ct);
        var sources = await db.CrmLeadSources.Where(s => s.DeletedAt == null)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .Select(s => new { crmLeadSourceId = s.CrmLeadSourceId, name = s.Name })
            .ToListAsync(ct);
        return Results.Ok(new { partners, programmes, sources });
    }
}
