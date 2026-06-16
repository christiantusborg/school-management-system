using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(e => e.SubjectId);

        // ECTS with one decimal place so half-points (7.5) are storable.
        builder.Property(e => e.Ects).HasPrecision(5, 1);

        builder.HasOne(e => e.Specializations)
            .WithMany(s => s.Subject)
            .HasForeignKey(e => e.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
