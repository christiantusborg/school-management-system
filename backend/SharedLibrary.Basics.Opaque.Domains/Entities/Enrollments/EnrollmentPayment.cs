namespace SharedLibrary.Basics.Opaque.Domains;

public class EnrollmentPayment
{
    public Guid StudentEnrollmentPaymentId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public DateTime PaymentDueDate { get; set; }
    public decimal PaymentDueAmount { get; set; }
    public DateTime? PaymentDateAt { get; set; }
}