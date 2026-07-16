using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A certificate of cooperation/partnership issued to a partner for one of the
/// MGW schools (one active certificate per (partner, school)). Designed in the
/// same visual certificate editor as student letters; the PDF is rendered live
/// on download for both the Admission Office and the partner portal.
/// </summary>
public class PartnerCertificate : IDeletedAtEntity
{
    public Guid PartnerCertificateId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid SchoolId { get; set; }

    /// <summary>Display name, e.g. "Certificate of Partnership" — editable.</summary>
    public string Title { get; set; } = "Certificate of Partnership";

    /// <summary>Konva/QuestPDF layout (same schema as letter templates).
    /// Seeded with the starter partnership design on create.</summary>
    public string? CertificateLayoutJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
