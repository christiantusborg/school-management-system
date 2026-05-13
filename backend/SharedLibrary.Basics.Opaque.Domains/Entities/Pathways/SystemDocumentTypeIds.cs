namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Stable Guids for the seeded system-generated <see cref="DocumentType"/>
/// rows that back released letter PDFs. Lets the letter-release pipeline
/// look up the right DocumentType without a name round-trip.
/// </summary>
public static class SystemDocumentTypeIds
{
    public static readonly Guid OfferLetter            = Guid.Parse("22222222-2222-2222-2222-100000000001");
    public static readonly Guid AdmissionLetter        = Guid.Parse("22222222-2222-2222-2222-100000000002");
    public static readonly Guid Transcript             = Guid.Parse("22222222-2222-2222-2222-100000000003");
    public static readonly Guid Certificate            = Guid.Parse("22222222-2222-2222-2222-100000000004");
    public static readonly Guid ProvisionalCertificate = Guid.Parse("22222222-2222-2222-2222-100000000005");

    public sealed record Seed(Guid Id, string Name, string Description);

    public static readonly Seed[] All =
    {
        new(OfferLetter,            "Offer Letter",            "System-generated offer letter PDF."),
        new(AdmissionLetter,        "Admission Letter",        "System-generated admission letter PDF."),
        new(Transcript,             "Transcript",              "System-generated transcript PDF."),
        new(Certificate,            "Certificate",             "System-generated certificate PDF (with stamp/signature)."),
        new(ProvisionalCertificate, "Provisional Certificate", "System-generated certificate variant without stamp/signature."),
    };
}
