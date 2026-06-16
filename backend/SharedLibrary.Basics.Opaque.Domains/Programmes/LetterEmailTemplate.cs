using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Email that accompanies a released letter. One row per (programme × letter
/// type), edited alongside the letter template. Only Offer and Admission
/// letters carry an email today. The released PDF is attached and the
/// subject/body support the same tag tokens as the letter itself.
/// </summary>
public class LetterEmailTemplate : IDeletedAtEntity
{
    public Guid LetterEmailTemplateId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }

    /// <summary>
    /// Owning partner. Templates are per (programme, partner, letter type) so
    /// each partner offering a shared core programme has its own independent
    /// email (recipients + body). No cross-partner fallback.
    /// </summary>
    public Guid PartnerId { get; set; }

    public LetterType LetterType { get; set; }

    /// <summary>
    /// Master on/off for emailing this (programme, letter type). When false,
    /// release sends nothing and the manual Send action reports it as disabled.
    /// Defaults false so no mail leaves until an admin opts in.
    /// </summary>
    public bool IsEmailEnabled { get; set; }

    /// <summary>Subject line. Supports letter tags, e.g. "[student full name]".</summary>
    public string? Subject { get; set; }

    /// <summary>HTML body including signature. Supports letter tags.</summary>
    public string? BodyHtml { get; set; }

    /// <summary>JSON array of { email, enabled } for the default CC list.</summary>
    public string? CcRecipientsJson { get; set; }

    /// <summary>JSON array of { email, enabled } for the default BCC list.</summary>
    public string? BccRecipientsJson { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Programme Programme { get; set; } = default!;
}
