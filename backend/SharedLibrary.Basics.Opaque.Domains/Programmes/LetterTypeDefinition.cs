using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A config-created letter type (System Config → Letter Types,
/// SuperAdministrator only). Runs alongside the built-in LetterType enum
/// letters; the enum types migrate into rows here later. Each definition
/// owns a DocumentType row so released PDFs slot into StudentDocuments
/// unchanged.
/// </summary>
public class LetterTypeDefinition : IDeletedAtEntity
{
    public Guid LetterTypeDefinitionId { get; set; } = Guid.NewGuid();

    /// <summary>Display name, e.g. "Completion Confirmation".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code printed in the reference, e.g. "CC" in
    /// MGW-CC-A1B2C3D4. Uppercase letters/digits.</summary>
    public string ReferencePrefix { get; set; } = string.Empty;

    /// <summary>The StudentDocuments type this letter releases into;
    /// created together with the definition.</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>Student status that auto-generates the letter ONCE, the
    /// first time an enrolment reaches it. Null = manual Generate only.</summary>
    public Guid? TriggerStatusId { get; set; }

    public bool VisibleToStudent { get; set; }
    public bool VisibleToPartner { get; set; }

    /// <summary>Send the accompanying letter email on release (email body
    /// authored per programme+partner, like offer/admission letters).</summary>
    public bool EmailOnRelease { get; set; }

    /// <summary>Staff may upload an OLD scanned PDF instead of generating
    /// (pre-Hub graduates); Generate replaces the upload again.</summary>
    public bool AllowLegacyUpload { get; set; }

    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
