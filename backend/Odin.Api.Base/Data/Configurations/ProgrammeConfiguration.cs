using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> builder)
    {
        builder.HasKey(e => e.ProgrammeId);
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.Code)
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();

        builder.HasOne(e => e.Owner)
            .WithMany(p => p.Programmes)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Specializations)
            .WithOne(s => s.Programmes)
            .HasForeignKey(s => s.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AwardEducationLevel)
            .WithMany()
            .HasForeignKey(e => e.AwardEducationLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
