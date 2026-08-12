using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class Partner : IDeletedAtEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>
    /// Auto-generated portal identifier ("PA-YYYYMMDD-RAND6"), assigned when
    /// the partner is created by the Admission office. Same shape as
    /// Student.StudentNumber; uniqueness enforced by a unique index.
    /// </summary>
    public string PartnerNumber { get; set; } = default!;

    /// <summary>
    /// Controls where enrolments created by the admin CSV student import
    /// land for this partner. True (default): directly admitted, skipping
    /// the offer/admission review pipeline. False: they enter the normal
    /// admission queue (Awaiting Review by Admission).
    /// </summary>
    public bool ImportDirectAdmission { get; set; } = true;

    // Organisation identity
    public string? Website { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }

    /// <summary>
    /// Short human code / abbreviation for the partner (e.g. "IBAS" for
    /// International Business Academy of Switzerland). Set by the Admission
    /// office on the partner profile. Used in generated Faculty / datasheet
    /// auto-ids in place of the full partner name; falls back to a
    /// name-derived token when empty.
    /// </summary>
    public string? ShortCode { get; set; }

    // Profile-tab fields previously dropped on save (no domain columns).
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    /// <summary>Partnership tier label (e.g. Silver / Gold).</summary>
    public string? Tier { get; set; }
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Partner-level "disabled" flag. Distinct from <see cref="DeletedAt"/>:
    /// a disabled partner is invisible to its own users (they cannot log in)
    /// but still appears in the admin list and can be re-enabled. A deleted
    /// partner is soft-deleted via <see cref="DeletedAt"/> and only appears
    /// when the admin toggles "Show deleted". Null = enabled.
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<PartnerAddress> Addresses { get; set; } = new List<PartnerAddress>();
    public ICollection<PartnerContactPhone> Phones { get; set; } = new List<PartnerContactPhone>();
    public ICollection<PartnerContactEmail> Emails { get; set; } = new List<PartnerContactEmail>();
    public ICollection<PartnerContract> Contracts { get; set; } = new List<PartnerContract>();
    public ICollection<PartnerUsers> Users { get; set; } = new List<PartnerUsers>();
    public ICollection<Programme> Programmes { get; set; } = new List<Programme>();
}