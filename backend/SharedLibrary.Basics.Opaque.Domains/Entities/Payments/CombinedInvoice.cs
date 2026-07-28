using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// A partner's combined invoice: several students' unpaid payment items
/// (plan installments / additional invoices) bundled into ONE numbered
/// invoice the partner pays on their students' behalf. Status Open until
/// Admission marks it paid, which marks every underlying item paid.
/// </summary>
public class CombinedInvoice : IDeletedAtEntity
{
    public const string StatusOpen = "Open";
    public const string StatusPaid = "Paid";

    public Guid CombinedInvoiceId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    /// <summary>INV-{PARTNER}-{seq:D3}; sequence per partner, never reissued.</summary>
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = StatusOpen;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One line = one underlying student payment item, with a snapshot
/// of the display fields so the invoice stays stable over time.</summary>
public class CombinedInvoiceLine
{
    public Guid CombinedInvoiceLineId { get; set; } = Guid.NewGuid();
    public Guid CombinedInvoiceId { get; set; }
    /// <summary>Exactly one of these two is set.</summary>
    public Guid? PaymentInstallmentId { get; set; }
    public Guid? AdditionalInvoiceId { get; set; }
    public Guid StudentEnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string ProgrammeCode { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
