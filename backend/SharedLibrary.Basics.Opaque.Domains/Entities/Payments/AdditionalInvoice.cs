namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// A manually-created one-off invoice attached to an enrolment's payment plan
/// (attestation fee, delivery fee, reprinting fee, …). Unlike tuition it is
/// never split into installments: it is paid as a whole. Carries one or more
/// lines (description + amount) and the same per-invoice payment methods as a
/// <see cref="PaymentInstallment"/>.
/// </summary>
public class AdditionalInvoice
{
    public Guid AdditionalInvoiceId { get; set; } = Guid.NewGuid();
    public Guid PaymentPlanId { get; set; }

    /// <summary>1-based position among the plan's additional invoices — used
    /// for numbering ("Additional invoice 2") and stable ordering.</summary>
    public int Sequence { get; set; }

    public DateTime? DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }

    /// <summary>When true, this invoice's PDF carries the credit-card
    /// payment link below.</summary>
    public bool PayByCardEnabled { get; set; }
    public string? CardPaymentLink { get; set; }

    /// <summary>When true, this invoice's PDF carries the bank-transfer
    /// details below.</summary>
    public bool PayByBankEnabled { get; set; }
    public string? BankAccountDetails { get; set; }

    /// <summary>Invoice lines as a JSON array: [{"text":"…","amount":0.00}, …].
    /// Always at least one line. The invoice total is the sum of the lines.</summary>
    public string LinesJson { get; set; } = "[]";

    public EnrollmentPaymentPlan Plan { get; set; } = default!;
}
