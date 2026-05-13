namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Join table: a pathway accepts a given prior education level. The wizard
/// only offers a pathway if either (a) the pathway has no rows here (no
/// restriction) or (b) the student's chosen highest degree appears here.
/// </summary>
public class PathwayAcceptedEducationLevel
{
    public Guid PathwayId { get; set; }
    public Guid EducationLevelId { get; set; }
    
    public Pathway Pathway { get; set; } = default!;
    public EducationLevel EducationLevel { get; set; } = default!;
}
