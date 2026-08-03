using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Per-SPECIALIZATION grant of a core programme to a partner. Row present =
/// admin granted this specialization; <see cref="DisabledByPartner"/> lets
/// the partner switch an offered specialization off without losing the
/// grant. The programme-level <see cref="ProgrammePartner"/> row is kept as
/// a derived umbrella (exists while the partner holds at least one spec
/// grant) because enrolment/import/cohort validation checks it.
/// </summary>
public class SpecializationPartner
{
    public Guid SpecializationPartnerId { get; set; } = Guid.NewGuid();
    public Guid SpecializationId { get; set; }
    public Guid PartnerId { get; set; }
    public bool DisabledByPartner { get; set; }
    public DateTime GrantedAt { get; set; }

    public Specialization Specialization { get; set; } = default!;
    public Partner Partner { get; set; } = default!;
}
