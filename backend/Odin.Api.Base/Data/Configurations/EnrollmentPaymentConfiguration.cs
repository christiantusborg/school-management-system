using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentPaymentConfiguration : IEntityTypeConfiguration<EnrollmentPayment>
{
    public void Configure(EntityTypeBuilder<EnrollmentPayment> builder)
    {
        builder.HasKey(e => e.StudentEnrollmentPaymentId);
        builder.Property(e => e.PaymentDueAmount).HasPrecision(18, 2);
        builder.HasOne<Enrollment>()
            .WithMany()
            .HasForeignKey(e => e.StudentEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
