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
    public static readonly Guid PrintableTranscript    = Guid.Parse("22222222-2222-2222-2222-100000000006");
    public static readonly Guid StudentIdCard          = Guid.Parse("22222222-2222-2222-2222-100000000007");
    public static readonly Guid StudentCardPicture     = Guid.Parse("22222222-2222-2222-2222-100000000008");
    public static readonly Guid FinalProposalApproval  = Guid.Parse("22222222-2222-2222-2222-100000000009");
    public static readonly Guid FinalProjectApproval   = Guid.Parse("22222222-2222-2222-2222-100000000010");

    public sealed record Seed(Guid Id, string Name, string Description);

    public static readonly Seed[] All =
    {
        new(OfferLetter,            "Offer Letter",            "System-generated offer letter PDF."),
        new(AdmissionLetter,        "Admission Letter",        "System-generated admission letter PDF."),
        new(Transcript,             "Digital Transcript",      "System-generated transcript PDF for the partner and student."),
        new(Certificate,            "Certificate",             "System-generated certificate PDF (with stamp/signature)."),
        new(ProvisionalCertificate, "Provisional Certificate", "System-generated certificate variant without stamp/signature."),
        new(PrintableTranscript,    "Printable Transcript",    "System-generated transcript PDF for the Admission Office only."),
        new(StudentIdCard,          "Student ID Card",         "System-generated digital student ID card PDF."),
        new(StudentCardPicture,     "Student Card Picture",    "Portrait photo the student uploads for the digital student ID card."),
        new(FinalProposalApproval,  "Final Project Proposal Approval Letter", "System-generated approval letter released when the dissertation/final project PROPOSAL mark reaches the programme's pass mark."),
        new(FinalProjectApproval,   "Final Project Approval Letter",          "System-generated approval letter released when the dissertation/final PROJECT mark reaches the programme's pass mark."),
    };
}
