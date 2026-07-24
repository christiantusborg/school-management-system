using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A grading rubric: a named list of weighted criteria rows. Shared
/// templates (IsShared) are built once in System Config and reusable across
/// modules; a module may instead own a private rubric (OwnerSubjectId set)
/// edited inline on the Academic page. Row weights must total 100.
/// </summary>
public class RubricTemplate : IDeletedAtEntity
{
    public Guid RubricTemplateId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    /// <summary>Shared templates show in System Config and the module rubric
    /// picker; non-shared ones belong to exactly one subject.</summary>
    public bool IsShared { get; set; }
    public Guid? OwnerSubjectId { get; set; }
    /// <summary>Set when this is a cohort-owned override rubric.</summary>
    public Guid? OwnerCohortId { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One criterion row (Section / Criteria / Max %). Graders score it
/// 1–100; it contributes MaxPercent% of the final module grade. Soft-deleted
/// rows keep saved scores; re-adding the same section+criteria restores.</summary>
public class RubricRow : IDeletedAtEntity
{
    public Guid RubricRowId { get; set; } = Guid.NewGuid();
    public Guid RubricTemplateId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty;
    /// <summary>Weight: share of the final grade this row carries (1–100).</summary>
    public int MaxPercent { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>The per-row scores behind one rubric-graded module grade — the
/// extra dimension on SubjectGrade. The grade's Score stays the single
/// number on sheets and transcripts (weighted total, rounded).</summary>
public class SubjectGradeRubricScore
{
    public Guid SubjectGradeRubricScoreId { get; set; } = Guid.NewGuid();
    public Guid SubjectGradeId { get; set; }
    public Guid RubricRowId { get; set; }
    public int Score { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
