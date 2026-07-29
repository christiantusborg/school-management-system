namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>Per-partner design template for combined invoices, authored in
/// the letter designer (same CertificateLayout JSON). When present it
/// replaces the built-in combined-invoice PDF for that partner.</summary>
public class CombinedInvoiceTemplate
{
    public Guid CombinedInvoiceTemplateId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public string CertificateLayoutJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
