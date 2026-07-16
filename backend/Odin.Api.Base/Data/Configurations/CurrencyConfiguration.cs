using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Payments;

namespace Odin.Api.Base.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(e => e.CurrencyId);
        builder.Property(e => e.Code).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(80);
    }
}
