using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class Programme : IDeletedAtEntity
{
    public Guid ProgrammeId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    
    
    public DateTime? DeletedAt { get; set; }
   
    public Guid? OwnerId { get; set; }
    public ICollection<Specialization> Specializations { get; set; } = [];
    public ICollection<ProgrammeDocumentRequirement> RequiredDocumentTypes { get; set; } = [];
    public Partner Owner { get; set; }

    /// <summary>
    /// Education level this programme awards on completion (e.g. BBA → Bachelor's,
    /// MBA → Master's, DBA → Doctorate). Used by the signup wizard to chain
    /// pathway eligibility across multi-degree applications.
    /// </summary>
    public Guid? AwardEducationLevelId { get; set; }
    public EducationLevel? AwardEducationLevel { get; set; }
}