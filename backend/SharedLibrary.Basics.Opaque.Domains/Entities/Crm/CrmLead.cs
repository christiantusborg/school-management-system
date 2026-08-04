using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Crm;

/// <summary>
/// CRM pipeline (e.g. "Student Recruitment", per-campaign funnels). Stages
/// are the kanban columns. Concepts adapted from the QuVian core CRM,
/// reimplemented natively for the IBSS Sales portal.
/// </summary>
public class CrmPipeline : IDeletedAtEntity
{
    public Guid CrmPipelineId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>Kanban column. StageType marks terminal columns; SlaHours drives
/// the "rotting" red flag when a lead sits in the stage too long.</summary>
public class CrmStage : IDeletedAtEntity
{
    public Guid CrmStageId { get; set; } = Guid.NewGuid();
    public Guid CrmPipelineId { get; set; }
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>0 = Open, 1 = Won (converted), 2 = Lost.</summary>
    public int StageType { get; set; }
    public int? SlaHours { get; set; }
    public DateTime? DeletedAt { get; set; }

    public CrmPipeline Pipeline { get; set; } = default!;
}

/// <summary>Configurable lead source list (Website, Facebook, Referral, …).</summary>
public class CrmLeadSource : IDeletedAtEntity
{
    public Guid CrmLeadSourceId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// A prospective student moving through a pipeline. Sales users see leads
/// they created or are assigned; Admission sees all. Conversion opens the
/// signup wizard prefilled; when the signup completes the lead links to the
/// student and flips to the pipeline's Won stage.
/// </summary>
public class CrmLead : IDeletedAtEntity
{
    public Guid CrmLeadId { get; set; } = Guid.NewGuid();
    public Guid CrmPipelineId { get; set; }
    public Guid CrmStageId { get; set; }
    /// <summary>0 = Open, 1 = Converted, 2 = Lost (kept in sync with the
    /// stage's StageType on stage moves).</summary>
    public int Status { get; set; }

    public string Name { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Note { get; set; }

    public Guid? CrmLeadSourceId { get; set; }
    /// <summary>Partner the future student would sign up under; required at
    /// conversion time (the wizard needs a partner).</summary>
    public Guid? PartnerId { get; set; }
    /// <summary>Programme the lead is interested in (prefills the wizard).</summary>
    public Guid? ProgrammeId { get; set; }
    /// <summary>0 = &lt;$1k, 1 = $1–5k, 2 = $5k+; null = unknown.</summary>
    public int? ValueBand { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid CreatedByUserId { get; set; }

    /// <summary>Deterministic score from structural signals; ≥ 70 = hot.</summary>
    public int Score { get; set; }
    public DateTime StageEnteredAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? ConvertedStudentId { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string? LostReason { get; set; }

    public DateTime? DeletedAt { get; set; }

    public CrmPipeline Pipeline { get; set; } = default!;
    public CrmStage Stage { get; set; } = default!;
    public CrmLeadSource? Source { get; set; }
    public ICollection<CrmActivity> Activities { get; set; } = new List<CrmActivity>();
}

/// <summary>
/// Unified per-lead timeline entry: interactions (note/call/email/WhatsApp/
/// meeting), tasks and system events. An entry with DueAt and no CompletedAt
/// is an open follow-up — the soonest one is the lead's "next action".
/// </summary>
public class CrmActivity
{
    public Guid CrmActivityId { get; set; } = Guid.NewGuid();
    public Guid CrmLeadId { get; set; }
    /// <summary>0 Note, 1 Call, 2 Email, 3 WhatsApp, 4 Meeting, 5 Task, 6 System.</summary>
    public int Kind { get; set; }
    public string? Body { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public CrmLead Lead { get; set; } = default!;
}

/// <summary>Single-row auto-assignment config: Manual or RoundRobin over a
/// comma-separated Sales-user pool with a persisted rotation cursor.</summary>
public class CrmAssignmentConfig
{
    public int CrmAssignmentConfigId { get; set; } = 1;
    /// <summary>0 = Manual, 1 = RoundRobin.</summary>
    public int Strategy { get; set; }
    public string? MemberUserIds { get; set; }
    public int LastAssignedIndex { get; set; } = -1;
}
