using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A published public form: an anonymous visitor opens /f/{slug} (typically
/// in an iframe on the school website) and fills the linked questionnaire.
/// Ported from QuVian core's funnel minus the parts IBSS explicitly does
/// not want: no KEM escrow (answers stored plain), no account registration,
/// and payments disabled (price fields kept for future use; see
/// <see cref="PublicFormPayment"/>).
/// </summary>
public class PublicForm : IDeletedAtEntity
{
    public Guid PublicFormId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>Opaque URL token for the public fill page (/f/{slug}).</summary>
    public string Slug { get; set; } = null!;
    public Guid QuestionnaireTemplateId { get; set; }
    public Guid? DocumentTemplateId { get; set; }

    /// <summary>A "run" of the form: the period it collects for. Start is
    /// informational (labels/groups runs of the same questionnaire); End is
    /// enforced — once past, the public link is closed and rejects
    /// submissions. Null = no window (always open). Multiple public forms of
    /// the same questionnaire are the separate runs, grouped in statistics.</summary>
    public DateTime? RunStartDate { get; set; }
    public DateTime? RunEndDate { get; set; }

    /// <summary>Per-form values for the questionnaire's owner-set reference
    /// fields (school / partner / partner→programme / free text), as jsonb.
    /// Shape: <c>[{ "fieldId", "type", "value", "partnerValue", "display" }]</c>.
    /// The owner fills these on the public-form dialog; on submit they are
    /// stamped into the submission's answers so they aggregate generically in
    /// statistics. Null = the questionnaire has no reference fields.</summary>
    public string? OwnerReferencesJson { get; set; }

    public int PriceAmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsPublished { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public QuestionnaireTemplate QuestionnaireTemplate { get; set; } = null!;
    public DocumentTemplate? DocumentTemplate { get; set; }
}

/// <summary>
/// An anonymous submission from the public fill page. Answers are plain
/// JSON by explicit decision (no KEM/field encryption in the intake
/// feature). Contact fields are whatever the form chose to ask for.
/// </summary>
public class PublicFormSubmission : IDeletedAtEntity
{
    public Guid PublicFormSubmissionId { get; set; } = Guid.NewGuid();
    public Guid PublicFormId { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public string? QuestionnaireVersionHash { get; set; }
    /// <summary>Optional respondent contact, when the form collects it.</summary>
    public string? RespondentEmail { get; set; }
    public string? RespondentName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public PublicForm PublicForm { get; set; } = null!;
}

/// <summary>
/// Payment record for a public-form submission. Schema ported from QuVian
/// core but the feature is disabled: no payment provider is wired, so no
/// rows are written today. Kept so enabling payments later is a code
/// change, not a schema change.
/// </summary>
public class PublicFormPayment
{
    public Guid PublicFormPaymentId { get; set; } = Guid.NewGuid();
    public Guid PublicFormSubmissionId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    /// <summary>"NotConfigured" until a provider is wired.</summary>
    public string Status { get; set; } = "NotConfigured";
    public string? ProviderReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PublicFormSubmission PublicFormSubmission { get; set; } = null!;
}
