namespace SharedLibrary.Basics.Opaque.Domains.Intake;

/// <summary>
/// A frozen, immutable snapshot of a questionnaire version that has received
/// submitted answers. Created automatically the moment an admin saves changes
/// to an answered questionnaire: the old definition lands here, the template
/// row becomes the next version. Statistics aggregate across the current
/// definition and these snapshots by question id.
/// </summary>
public class QuestionnaireTemplateVersion
{
    public Guid QuestionnaireTemplateVersionId { get; set; } = Guid.NewGuid();
    public Guid QuestionnaireTemplateId { get; set; }
    public string Version { get; set; } = null!;
    public string DefinitionJson { get; set; } = null!;
    public string DefinitionHash { get; set; } = null!;
    public DateTime FrozenAt { get; set; } = DateTime.UtcNow;

    public QuestionnaireTemplate Template { get; set; } = null!;
}
