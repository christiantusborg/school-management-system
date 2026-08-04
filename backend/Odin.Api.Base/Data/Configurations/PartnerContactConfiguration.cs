using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerContactConfiguration : IEntityTypeConfiguration<PartnerContact>
{
    public void Configure(EntityTypeBuilder<PartnerContact> builder)
    {
        builder.HasKey(e => e.PartnerContactId);
        builder.Property(e => e.Name).HasMaxLength(300);
        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Type)
            .WithMany()
            .HasForeignKey(e => e.PartnerContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.PartnerId);
    }
}

public class PartnerContactMethodConfiguration : IEntityTypeConfiguration<PartnerContactMethod>
{
    public void Configure(EntityTypeBuilder<PartnerContactMethod> builder)
    {
        builder.HasKey(e => e.PartnerContactMethodId);
        builder.Property(e => e.Value).HasMaxLength(500);
        builder.HasOne(e => e.Contact)
            .WithMany(c => c.Methods)
            .HasForeignKey(e => e.PartnerContactId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.MethodType)
            .WithMany()
            .HasForeignKey(e => e.ContactMethodTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContactMethodTypeConfiguration : IEntityTypeConfiguration<ContactMethodType>
{
    public void Configure(EntityTypeBuilder<ContactMethodType> builder)
    {
        builder.HasKey(e => e.ContactMethodTypeId);
        builder.Property(e => e.Name).HasMaxLength(120);
    }
}

public class PartnerContactTypeConfiguration : IEntityTypeConfiguration<PartnerContactType>
{
    public void Configure(EntityTypeBuilder<PartnerContactType> builder)
    {
        builder.HasKey(e => e.PartnerContactTypeId);
        builder.Property(e => e.Name).HasMaxLength(120);
    }
}
