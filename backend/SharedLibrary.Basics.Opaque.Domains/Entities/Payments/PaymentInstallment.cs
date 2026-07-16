namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// One scheduled tuition installment within an <see cref="EnrollmentPaymentPlan"/>.
/// The schedule is auto-split from the total on creation, then editable. The
/// Admission Office marks each one paid with a date.
/// </summary>
public class PaymentInstallment
{
    public Guid PaymentInstallmentId { get; set; } = Guid.NewGuid();
    public Guid PaymentPlanId { get; set; }

    /// <summary>1-based position in the schedule (1st payment, 2nd payment, …).</summary>
    public int Sequence { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }

    /// <summary>When true, this installment's invoice carries the credit-card
    /// payment link below.</summary>
    public bool PayByCardEnabled { get; set; }
    public string? CardPaymentLink { get; set; }

    /// <summary>When true, this installment's invoice carries the bank-transfer
    /// details below.</summary>
    public bool PayByBankEnabled { get; set; }
    public string? BankAccountDetails { get; set; }

    public EnrollmentPaymentPlan Plan { get; set; } = default!;
}
