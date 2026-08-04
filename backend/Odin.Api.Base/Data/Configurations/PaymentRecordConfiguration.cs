using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Payments;

namespace Odin.Api.Base.Data.Configurations;

public class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.HasKey(e => e.PaymentRecordId);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Note).HasMaxLength(1000);
        builder.HasOne(e => e.Installment)
            .WithMany()
            .HasForeignKey(e => e.PaymentInstallmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.AdditionalInvoice)
            .WithMany()
            .HasForeignKey(e => e.AdditionalInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.PaymentInstallmentId);
        builder.HasIndex(e => e.AdditionalInvoiceId);
    }
}
