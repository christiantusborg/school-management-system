using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class LetterTemplate : IDeletedAtEntity
{
    public Guid LetterTemplateId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public LetterType LetterType { get; set; }

    public string? BodyHtml { get; set; }

    public string? CertificateBackgroundPath { get; set; }
    public string? CertificateLayoutJson { get; set; }

    /// <summary>
    /// Gate for letter release. Seeded layouts start unpublished; the admin
    /// must explicitly save the template via the editor to flip this true,
    /// after which <see cref="LetterReleaseService"/> will render PDFs for
    /// this (programme, type). Releases are silently skipped when false.
    /// </summary>
    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Programme Programme { get; set; } = default!;
}
