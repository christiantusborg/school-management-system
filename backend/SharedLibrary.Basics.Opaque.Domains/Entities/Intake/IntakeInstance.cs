using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// Attaches a questionnaire to an IBSS fill audience (ported from the QuVian
/// core intake plane, where instances attach to cases instead). The
/// <see cref="Audience"/> decides who sees and fills it: "Student" (student
/// portal), "Partner" (partner portal) or "SignupWizard" (extra survey step
/// in the public signup flow). Either references a
/// <see cref="QuestionnaireTemplate"/> or carries a one-off form inline via
/// <see cref="InlineDefinitionJson"/>. <see cref="OutputProfileJson"/> is
/// reserved for the output-generation phase and defaults to "{}".
/// </summary>
public class IntakeInstance : IDeletedAtEntity
{
    public const string AudienceStudent = "Student";
    public const string AudiencePartner = "Partner";
    public const string AudienceSignupWizard = "SignupWizard";

    public Guid IntakeInstanceId { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string Audience { get; set; } = AudienceStudent;
    public bool IsActive { get; set; } = true;
    public Guid? QuestionnaireTemplateId { get; set; }
    public string? InlineDefinitionJson { get; set; }
    public string OutputProfileJson { get; set; } = "{}";
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public QuestionnaireTemplate? QuestionnaireTemplate { get; set; }
}
