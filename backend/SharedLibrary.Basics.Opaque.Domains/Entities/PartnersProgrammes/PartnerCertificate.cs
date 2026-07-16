using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A certificate of cooperation/partnership issued to a partner for one of the
/// MGW schools (one active certificate per (partner, school)). Designed in the
/// same visual certificate editor as student letters; the PDF is rendered live
/// on download for both the Admission Office and the partner portal.
/// </summary>
public enum PartnerCertificateKind
{
    /// <summary>Decorative certificate of partnership/cooperation.</summary>
    Certificate = 0,

    /// <summary>Formal letter authorizing the partner to promote programmes
    /// and recruit students for the school ("Letter of Authorization -
    /// Approved Admissions Partner").</summary>
    AuthorizationLetter = 1,
}

public class PartnerCertificate : IDeletedAtEntity
{
    public Guid PartnerCertificateId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid SchoolId { get; set; }

    /// <summary>Certificate or authorization letter — one of each kind
    /// allowed per (partner, school).</summary>
    public PartnerCertificateKind Kind { get; set; } = PartnerCertificateKind.Certificate;

    /// <summary>Display name, e.g. "Certificate of Partnership" — editable.</summary>
    public string Title { get; set; } = "Certificate of Partnership";

    /// <summary>Konva/QuestPDF layout (same schema as letter templates).
    /// Seeded with the starter partnership design on create.</summary>
    public string? CertificateLayoutJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
