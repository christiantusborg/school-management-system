using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Which <see cref="DocumentType"/>s a given <see cref="Programme"/>
/// requires the student to upload as part of the application. Replaces
/// the previously hard-coded "canonical 4 slots" in the detail
/// endpoints — programmes can now demand different docs (e.g. MBA might
/// want a GMAT score while BBA does not).
/// </summary>
public class ProgrammeDocumentRequirement : IDeletedAtEntity
{
    public Guid ProgrammeDocumentRequirementId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Guid DocumentTypeId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Programme Programme { get; set; } = default!;
    public DocumentType DocumentType { get; set; } = default!;
}
