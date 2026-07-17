using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// A globally configured kind of partner document (System Config → Partner
/// Documents): certificates, authorization letters, diplomas, … Each type
/// carries ONE shared PDF template (designed in the certificate editor) and
/// the list of fill-out fields every document of this type asks for. Changing
/// the template instantly affects every partner document of the type.
/// </summary>
public class PartnerDocumentType : IDeletedAtEntity
{
    public Guid PartnerDocumentTypeId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Field definitions as JSON:
    /// [{"id","label","type":"text|date|image|partner","source"}].
    /// "text" = free text, "date" = calendar pick, "image" = uploaded file
    /// replacing a bound image placeholder in the template, "partner" =
    /// auto-filled from the partner profile via "source" (e.g. contractEnd).
    /// Each field is offered in the designer as the tag "[&lt;label&gt;]".</summary>
    public string FieldsJson { get; set; } = "[]";

    /// <summary>Certificate-editor layout shared by ALL documents of this type.</summary>
    public string? LayoutJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One issued document for a partner: a document type plus the filled-out
/// field values. Any number of documents per (partner, type). The PDF is
/// always rendered live from the type's current template.
/// </summary>
public class PartnerDocument : IDeletedAtEntity
{
    public Guid PartnerDocumentId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid PartnerDocumentTypeId { get; set; }

    /// <summary>{"&lt;fieldId&gt;":"&lt;value&gt;"} — text raw, date as ISO
    /// yyyy-MM-dd, image as the uploaded LetterAsset id.</summary>
    public string FieldValuesJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
