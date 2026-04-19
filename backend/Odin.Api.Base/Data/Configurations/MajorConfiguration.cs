using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class MajorConfiguration : IEntityTypeConfiguration<Major>
{
    public void Configure(EntityTypeBuilder<Major> builder)
    {
        builder.HasKey(e => e.MajorId);
        builder.HasOne(e => e.Programme)
            .WithMany(p => p.Majors)
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
