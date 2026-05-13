namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public enum LetterType
{
    OfferLetter = 0,
    AdmissionLetter = 1,
    Transcript = 2,
    Certificate = 3,
    /// <summary>Certificate variant without stamp/signature — typically a
    /// provisional/preview certificate the student or employer can hold
    /// alongside the formally stamped one.</summary>
    ProvisionalCertificate = 4,
}
