using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A rule that, given a set of answers, decides which
/// <see cref="DocumentTemplate"/>s should be offered/generated. The
/// <see cref="RuleJson"/> payload is a LogicExpression compatible with the
/// questionnaire renderer's conditional engine;
/// <see cref="IncludeDocumentTemplateIdsCsv"/> lists the DocumentTemplate
/// ids to include when the expression evaluates true (CSV, ported as-is
/// from QuVian core v1).
/// </summary>
public class GenerationRule : IDeletedAtEntity
{
    public Guid GenerationRuleId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string RuleJson { get; set; } = null!;
    public string IncludeDocumentTemplateIdsCsv { get; set; } = "";
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
