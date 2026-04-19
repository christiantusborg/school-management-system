using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class FinalProjectStatusConfiguration : IEntityTypeConfiguration<FinalProjectStatus>
{
    public void Configure(EntityTypeBuilder<FinalProjectStatus> builder)
    {
        builder.HasKey(e => e.FinalProjectStatusId);
        builder.Property(e => e.FinalProjectStatusId).ValueGeneratedNever();
        builder.HasData(
            new FinalProjectStatus { FinalProjectStatusId = 1, Name = "Not Started" },
            new FinalProjectStatus { FinalProjectStatusId = 2, Name = "In Progress" },
            new FinalProjectStatus { FinalProjectStatusId = 3, Name = "Submitted" },
            new FinalProjectStatus { FinalProjectStatusId = 4, Name = "Passed" },
            new FinalProjectStatus { FinalProjectStatusId = 5, Name = "Failed" });
    }
}
