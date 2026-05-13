using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class Specialization : IDeletedAtEntity
{
    public Guid SpecializationId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    
    public int DurationOfStudyMonths { get; set; }

    /// <summary>
    /// Total tuition fee in USD for completing this specialization.
    /// Read-only at review time — set per specialization, not per enrolment.
    /// </summary>
    public decimal TuitionFeeUsd { get; set; }

    public OfferAcceptanceMode OfferAcceptanceMode { get; set; } = OfferAcceptanceMode.StudentAccept;

    /// <summary>
    /// Free-text language the programme is taught in (e.g. "English",
    /// "Bilingual (English/Arabic)"). Surfaced in the [instruction language]
    /// letter tag.
    /// </summary>
    public string? InstructionLanguage { get; set; }


    public DateTime? IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
  

    public Programme Programmes { get; set; }
    public ICollection<Subject> Subject { get; set; } = [];
}