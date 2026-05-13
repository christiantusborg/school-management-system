namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerAddressType
{
    public Guid PartnerAddressTypeId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;   // "primary", "registered", "billing", "shipping"
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}