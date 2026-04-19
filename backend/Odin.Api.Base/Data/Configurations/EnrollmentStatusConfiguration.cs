using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentStatusConfiguration : IEntityTypeConfiguration<EnrollmentStatus>
{
    public void Configure(EntityTypeBuilder<EnrollmentStatus> builder)
    {
        builder.HasKey(e => e.EnrollmentStatusId);
        builder.Property(e => e.EnrollmentStatusId).ValueGeneratedNever();
        builder.HasData(
            new EnrollmentStatus { EnrollmentStatusId = 1,  Name = "Active" },
            new EnrollmentStatus { EnrollmentStatusId = 2,  Name = "Active (Final Project)" },
            new EnrollmentStatus { EnrollmentStatusId = 3,  Name = "Applicant Withdraw" },
            new EnrollmentStatus { EnrollmentStatusId = 4,  Name = "Cancelled" },
            new EnrollmentStatus { EnrollmentStatusId = 5,  Name = "Deferred" },
            new EnrollmentStatus { EnrollmentStatusId = 6,  Name = "Dismissed" },
            new EnrollmentStatus { EnrollmentStatusId = 7,  Name = "Drop Out" },
            new EnrollmentStatus { EnrollmentStatusId = 8,  Name = "Graduated" },
            new EnrollmentStatus { EnrollmentStatusId = 9,  Name = "Potential Applicant" },
            new EnrollmentStatus { EnrollmentStatusId = 10, Name = "Potential Applicant Paid" },
            new EnrollmentStatus { EnrollmentStatusId = 11, Name = "Transferred" });
    }
}
