using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

/// <summary>
/// Configurable channel MGW uses to reach partners (Email, Phone, WhatsApp,
/// WeChat, …). Managed in System Config → Contact Methods; "disabled" is the
/// soft-delete flag so the standard list manager's delete/restore acts as
/// disable/enable. Seeded with common worldwide channels; only Email, Phone
/// and WhatsApp start enabled.
/// </summary>
public class ContactMethodType : IDeletedAtEntity
{
    public Guid ContactMethodTypeId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Role a partner contact fills towards MGW (Owner, Admission, Marketing,
/// Finance, …). Managed in System Config alongside the contact methods.
/// </summary>
public class PartnerContactType : IDeletedAtEntity
{
    public Guid PartnerContactTypeId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// One named contact person at a partner, typed by role, carrying any number
/// of contact methods. Owner-typed contacts are managed by ADMISSION only;
/// partners manage every other type from their portal.
/// </summary>
public class PartnerContact
{
    public Guid PartnerContactId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Guid PartnerContactTypeId { get; set; }
    public string Name { get; set; } = default!;
    /// <summary>Free text: title, department, "call after 14:00", … .</summary>
    public string? Note { get; set; }
    public int SortOrder { get; set; }

    public Partner Partner { get; set; } = default!;
    public PartnerContactType Type { get; set; } = default!;
    public ICollection<PartnerContactMethod> Methods { get; set; } = new List<PartnerContactMethod>();
}

/// <summary>A single reachable value on a contact: (method type, value) —
/// e.g. (WhatsApp, +45 12 34 56 78).</summary>
public class PartnerContactMethod
{
    public Guid PartnerContactMethodId { get; set; } = Guid.NewGuid();
    public Guid PartnerContactId { get; set; }
    public Guid ContactMethodTypeId { get; set; }
    public string Value { get; set; } = default!;

    public PartnerContact Contact { get; set; } = default!;
    public ContactMethodType MethodType { get; set; } = default!;
}
