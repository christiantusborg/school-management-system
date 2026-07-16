using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// Lifecycle of an <see cref="IntakeResponse"/>. Draft = work-in-progress
/// visible only to the filler; Submitted = immutable and stamped with the
/// questionnaire version hash.
/// </summary>
public enum IntakeResponseLifecycleState
{
    Draft = 0,
    Submitted = 1,
}

/// <summary>
/// A single submission against an <see cref="IntakeInstance"/>. Unlike the
/// QuVian core original (per-recipient KEM wraps + Ed25519 signatures), IBSS
/// stores the answer payload as plain <see cref="AnswersJson"/> text — no
/// KEM or field encryption anywhere in the intake feature, by explicit
/// decision. Once <see cref="LifecycleState"/>
/// flips to Submitted the response is immutable; corrections become a new
/// response. <see cref="QuestionnaireVersionHash"/> is the template's
/// DefinitionHash in force at submit time, so it stays provable which
/// questions were asked. Exactly one of <see cref="StudentId"/> /
/// <see cref="PartnerId"/> identifies the respondent (both null only for
/// legacy/system rows); <see cref="CreatedByUserId"/> is set when the filler
/// had a signed-in account.
/// </summary>
public class IntakeResponse : IDeletedAtEntity
{
    public Guid IntakeResponseId { get; set; } = Guid.NewGuid();
    public Guid IntakeInstanceId { get; set; }
    public IntakeResponseLifecycleState LifecycleState { get; set; } = IntakeResponseLifecycleState.Draft;
    public DateTime? SubmittedAt { get; set; }
    public string? QuestionnaireVersionHash { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public string? CreatedByUserId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public IntakeInstance IntakeInstance { get; set; } = null!;
}
