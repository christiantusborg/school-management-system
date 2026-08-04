using System.Security.Claims;
using Odin.Api.Base.Authorization;
using SharedLibrary.Basics.Opaque.Domains.Crm;

namespace School.PartnerAdminApi.Admin.V1.Crm;

/// <summary>
/// CRM Settings (Administrator+ only): pipelines, stages (colour, SLA,
/// Open/Won/Lost type), lead sources and the Manual/RoundRobin assignment
/// pool. Reads are open to every admin level so the board can render.
/// </summary>
[Route("/v1/admin/crm/settings")]
[EndpointTag("Admin.Crm")]
public sealed class AdminV1CrmSettingsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/crm/settings", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/crm/settings/pipelines", PipelineCreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/crm/settings/pipelines/{id:guid}", PipelineUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/crm/settings/pipelines/{id:guid}", PipelineDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/crm/settings/pipelines/{id:guid}/stages", StagesReplaceAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/crm/settings/sources", SourceCreateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/crm/settings/sources/{id:guid}", SourceDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/crm/settings/assignment", AssignmentUpsertAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult?> RequireAdministratorAsync(
        HttpContext httpContext, UserManager<ApplicationUser> userManager)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null) return Results.Unauthorized();
        var ok = await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
        return ok ? null : Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
    }

    public sealed class PipelineBody { public string? Name { get; init; } public bool? IsDefault { get; init; } }
    public sealed class StageInput
    {
        public Guid? StageId { get; init; }
        public string? Name { get; init; }
        public string? Color { get; init; }
        public int StageType { get; init; }
        public int? SlaHours { get; init; }
    }
    public sealed class StagesBody { public List<StageInput>? Stages { get; init; } }
    public sealed class SourceBody { public string? Name { get; init; } }
    public sealed class AssignmentBody { public int Strategy { get; init; } public List<Guid>? MemberUserIds { get; init; } }

    private static async Task<IResult> GetAsync(OdinDbContext db, CancellationToken ct)
    {
        var pipelines = await db.CrmPipelines.Where(p => p.DeletedAt == null)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new { crmPipelineId = p.CrmPipelineId, name = p.Name, isDefault = p.IsDefault })
            .ToListAsync(ct);
        var stages = await db.CrmStages.Where(s => s.DeletedAt == null)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new { crmStageId = s.CrmStageId, pipelineId = s.CrmPipelineId, name = s.Name, color = s.Color, stageType = s.StageType, slaHours = s.SlaHours })
            .ToListAsync(ct);
        var sources = await db.CrmLeadSources.Where(s => s.DeletedAt == null)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .Select(s => new { crmLeadSourceId = s.CrmLeadSourceId, name = s.Name })
            .ToListAsync(ct);
        var cfg = await db.CrmAssignmentConfigs.FirstOrDefaultAsync(c => c.CrmAssignmentConfigId == 1, ct);
        return Results.Ok(new
        {
            pipelines,
            stages,
            sources,
            assignment = new
            {
                strategy = cfg?.Strategy ?? 0,
                memberUserIds = (cfg?.MemberUserIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            },
        });
    }

    private static async Task<IResult> PipelineCreateAsync(
        [FromBody] PipelineBody body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Name is required." });
        var pipeline = new CrmPipeline
        {
            Name = name,
            DisplayOrder = await db.CrmPipelines.CountAsync(ct) + 1,
        };
        db.CrmPipelines.Add(pipeline);
        // A new pipeline gets a starter stage set; editable afterwards.
        var defaults = new (string Name, string Color, int Type, int? Sla)[]
        {
            ("New", "#1058a4", 0, 24), ("Contacted", "#7a5c00", 0, 48), ("Interested", "#1c7a4a", 0, null),
            ("Applying", "#6b21a8", 0, null), ("Enrolled", "#14532d", 1, null), ("Lost", "#8a1515", 2, null),
        };
        var order = 0;
        foreach (var d in defaults)
            db.CrmStages.Add(new CrmStage
            {
                CrmPipelineId = pipeline.CrmPipelineId,
                Name = d.Name, Color = d.Color, StageType = d.Type, SlaHours = d.Sla, DisplayOrder = ++order,
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmPipelineId = pipeline.CrmPipelineId });
    }

    private static async Task<IResult> PipelineUpdateAsync(
        Guid id, [FromBody] PipelineBody body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var pipeline = await db.CrmPipelines.FirstOrDefaultAsync(p => p.CrmPipelineId == id && p.DeletedAt == null, ct);
        if (pipeline is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) pipeline.Name = body.Name.Trim();
        if (body.IsDefault == true)
        {
            foreach (var other in await db.CrmPipelines.Where(p => p.IsDefault).ToListAsync(ct))
                other.IsDefault = false;
            pipeline.IsDefault = true;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmPipelineId = id });
    }

    private static async Task<IResult> PipelineDeleteAsync(
        Guid id, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var pipeline = await db.CrmPipelines.FirstOrDefaultAsync(p => p.CrmPipelineId == id && p.DeletedAt == null, ct);
        if (pipeline is null) return Results.NotFound();
        if (await db.CrmLeads.AnyAsync(l => l.CrmPipelineId == id && l.DeletedAt == null, ct))
            return Results.BadRequest(new { error = "Pipeline has leads; move or delete them first." });
        pipeline.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmPipelineId = id });
    }

    /// <summary>Replace a pipeline's stage list. Stages carrying leads must
    /// survive (matched by StageId); dropping one with leads is refused.</summary>
    private static async Task<IResult> StagesReplaceAsync(
        Guid id, [FromBody] StagesBody body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var pipelineExists = await db.CrmPipelines.AnyAsync(p => p.CrmPipelineId == id && p.DeletedAt == null, ct);
        if (!pipelineExists) return Results.NotFound();
        var inputs = (body.Stages ?? new()).Where(s => !string.IsNullOrWhiteSpace(s.Name)).ToList();
        if (inputs.Count == 0) return Results.BadRequest(new { error = "At least one stage is required." });

        var existing = await db.CrmStages.Where(s => s.CrmPipelineId == id && s.DeletedAt == null).ToListAsync(ct);
        var keepIds = inputs.Where(i => i.StageId is not null).Select(i => i.StageId!.Value).ToHashSet();
        foreach (var gone in existing.Where(e => !keepIds.Contains(e.CrmStageId)))
        {
            if (await db.CrmLeads.AnyAsync(l => l.CrmStageId == gone.CrmStageId && l.DeletedAt == null, ct))
                return Results.BadRequest(new { error = $"Stage '{gone.Name}' still has leads and cannot be removed." });
            gone.DeletedAt = DateTime.UtcNow;
        }
        var order = 0;
        foreach (var i in inputs)
        {
            order++;
            var row = i.StageId is { } sid ? existing.FirstOrDefault(e => e.CrmStageId == sid) : null;
            if (row is null)
            {
                row = new CrmStage { CrmPipelineId = id };
                db.CrmStages.Add(row);
            }
            row.Name = i.Name!.Trim();
            row.Color = i.Color?.Trim();
            row.StageType = i.StageType is 1 or 2 ? i.StageType : 0;
            row.SlaHours = i.SlaHours;
            row.DisplayOrder = order;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmPipelineId = id });
    }

    private static async Task<IResult> SourceCreateAsync(
        [FromBody] SourceBody body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { error = "Name is required." });
        var src = new CrmLeadSource { Name = name, DisplayOrder = await db.CrmLeadSources.CountAsync(ct) + 1 };
        db.CrmLeadSources.Add(src);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadSourceId = src.CrmLeadSourceId });
    }

    private static async Task<IResult> SourceDeleteAsync(
        Guid id, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var src = await db.CrmLeadSources.FirstOrDefaultAsync(s => s.CrmLeadSourceId == id && s.DeletedAt == null, ct);
        if (src is null) return Results.NotFound();
        src.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { crmLeadSourceId = id });
    }

    private static async Task<IResult> AssignmentUpsertAsync(
        [FromBody] AssignmentBody body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        if (await RequireAdministratorAsync(httpContext, userManager) is { } fail) return fail;
        var cfg = await db.CrmAssignmentConfigs.FirstOrDefaultAsync(c => c.CrmAssignmentConfigId == 1, ct);
        if (cfg is null)
        {
            cfg = new CrmAssignmentConfig { CrmAssignmentConfigId = 1 };
            db.CrmAssignmentConfigs.Add(cfg);
        }
        cfg.Strategy = body.Strategy == 1 ? 1 : 0;
        cfg.MemberUserIds = body.MemberUserIds is { Count: > 0 }
            ? string.Join(',', body.MemberUserIds.Distinct())
            : null;
        cfg.LastAssignedIndex = -1;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { strategy = cfg.Strategy });
    }
}
