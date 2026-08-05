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
    /// <summary>Final transcript variant for the Admission Office only (not
    /// downloadable by the partner or student). Rendered from its own template
    /// and released at graduation alongside the (digital) Transcript.</summary>
    PrintableTranscript = 5,
    /// <summary>Digital student ID card. Only available when the programme's
    /// IssueDigitalStudentCard toggle is on; visible to the Admission Office,
    /// the partner and the student.</summary>
    StudentIdCard = 6,
    /// <summary>Approval letter for the Final Project Proposal / Dissertation
    /// Project Proposal — auto-released when the student's mark in a
    /// "Dissertation Proposal"-type cohort reaches the programme's pass mark.</summary>
    FinalProposalApproval = 7,
    /// <summary>Approval letter for the Final Project / Dissertation Project —
    /// auto-released when the student's mark in a "Final Project /
    /// Dissertation"-type cohort reaches the programme's pass mark.</summary>
    FinalProjectApproval = 8,
}
