namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// One recorded (part-)payment against a tuition installment OR an
/// additional invoice — exactly one parent is set. An item's IsPaid flag is
/// DERIVED: it flips to paid automatically once its records cover the full
/// amount (paid date = latest record's date) and back when they no longer
/// do. Note is free text ("where paid to", reference, …).
/// </summary>
public class PaymentRecord
{
    public Guid PaymentRecordId { get; set; } = Guid.NewGuid();

    public Guid? PaymentInstallmentId { get; set; }
    public Guid? AdditionalInvoiceId { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public PaymentInstallment? Installment { get; set; }
    public AdditionalInvoice? AdditionalInvoice { get; set; }
}
