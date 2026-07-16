using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Payments;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentPaymentPlanConfiguration : IEntityTypeConfiguration<EnrollmentPaymentPlan>
{
    public void Configure(EntityTypeBuilder<EnrollmentPaymentPlan> builder)
    {
        builder.HasKey(e => e.PaymentPlanId);
        builder.Property(e => e.TotalTuitionFee).HasPrecision(12, 2);
        builder.Property(e => e.Currency).HasMaxLength(8).IsRequired();
        // One active plan per enrolment.
        builder.HasIndex(e => e.StudentEnrollmentId)
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();

        builder.HasMany(e => e.Installments)
            .WithOne(i => i.Plan)
            .HasForeignKey(i => i.PaymentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AdditionalInvoices)
            .WithOne(i => i.Plan)
            .HasForeignKey(i => i.PaymentPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AdditionalInvoiceConfiguration : IEntityTypeConfiguration<AdditionalInvoice>
{
    public void Configure(EntityTypeBuilder<AdditionalInvoice> builder)
    {
        builder.HasKey(e => e.AdditionalInvoiceId);
        builder.Property(e => e.LinesJson).IsRequired();
    }
}

public class PaymentInstallmentConfiguration : IEntityTypeConfiguration<PaymentInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentInstallment> builder)
    {
        builder.HasKey(e => e.PaymentInstallmentId);
        builder.Property(e => e.Amount).HasPrecision(12, 2);
    }
}
