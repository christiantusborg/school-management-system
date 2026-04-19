using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class TuitionFeeStatusConfiguration : IEntityTypeConfiguration<TuitionFeeStatus>
{
    public void Configure(EntityTypeBuilder<TuitionFeeStatus> builder)
    {
        builder.HasKey(e => e.TuitionFeeStatusId);
        builder.Property(e => e.TuitionFeeStatusId).ValueGeneratedNever();
        builder.HasData(
            new TuitionFeeStatus { TuitionFeeStatusId = 1, Name = "Unpaid" },
            new TuitionFeeStatus { TuitionFeeStatusId = 2, Name = "Partially Paid" },
            new TuitionFeeStatus { TuitionFeeStatusId = 3, Name = "Fully Paid" },
            new TuitionFeeStatus { TuitionFeeStatusId = 4, Name = "Not Applicable" });
    }
}
