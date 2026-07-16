using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// Per-enrolment tuition payment plan managed by the Admission Office. Holds
/// the manually-entered total tuition fee and the number of installments; the
/// individual <see cref="PaymentInstallment"/> rows carry the schedule. Balance
/// is derived (total minus the sum of paid installments), not stored.
/// </summary>
public class EnrollmentPaymentPlan : IDeletedAtEntity
{
    public Guid PaymentPlanId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public decimal TotalTuitionFee { get; set; }

    public string Currency { get; set; } = "USD";
    public int NumberOfPayments { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PaymentInstallment> Installments { get; set; } = [];

    /// <summary>Manually-created one-off invoices (attestation fee, delivery
    /// fee, …) — never split into installments, paid as a whole.</summary>
    public ICollection<AdditionalInvoice> AdditionalInvoices { get; set; } = [];
}
