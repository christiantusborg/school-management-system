using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Pathway : IDeletedAtEntity
{
    public Guid PathwayId { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }

    public int MinimumYearsWorkExperience { get; set; }
    public ICollection<PathwayAcceptedEducationLevel> AcceptedEducationLevels { get; set; } = [];
    public ICollection<PathwayDocumentRequirement> DocumentRequirements { get; set; } = [];

    
}
