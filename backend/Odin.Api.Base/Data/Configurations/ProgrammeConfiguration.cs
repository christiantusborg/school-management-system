using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> builder)
    {
        builder.HasKey(e => e.ProgrammeId);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.PartnerId, e.Status });

        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ClonedFromProgramme)
            .WithMany()
            .HasForeignKey(e => e.ClonedFromProgrammeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Status).HasConversion<int>();
    }
}
