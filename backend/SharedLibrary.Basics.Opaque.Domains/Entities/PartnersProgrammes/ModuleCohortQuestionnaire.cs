using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A questionnaire (from the admin Questionnaires builder) attached to a
/// module cohort by the Admission Office. Students assigned to the cohort
/// must submit every attached questionnaire before the cohort's grade is
/// shown to them in the student portal.
/// </summary>
public class ModuleCohortQuestionnaire : IDeletedAtEntity
{
    public Guid ModuleCohortQuestionnaireId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortId { get; set; }
    public Guid QuestionnaireTemplateId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One submitted answer set for a cohort questionnaire. Deliberately carries
/// NO student/enrolment reference: answers are anonymous by design. Who has
/// completed what is tracked separately in
/// <see cref="CohortQuestionnaireCompletion"/>, so the two cannot be joined.
/// </summary>
public class CohortQuestionnaireResponse : IEntity
{
    public Guid CohortQuestionnaireResponseId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortQuestionnaireId { get; set; }
    public string AnswersJson { get; set; } = "{}";
    /// <summary>Template DefinitionHash at submit time (mirrors IntakeResponse).</summary>
    public string QuestionnaireVersionHash { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Marks that a student (enrolment) completed a cohort questionnaire. Drives
/// the grade gate and the completion counts; intentionally not linkable to a
/// specific <see cref="CohortQuestionnaireResponse"/> row.
/// </summary>
public class CohortQuestionnaireCompletion : IEntity
{
    public Guid CohortQuestionnaireCompletionId { get; set; } = Guid.NewGuid();
    public Guid ModuleCohortQuestionnaireId { get; set; }
    public Guid StudentEnrollmentId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
