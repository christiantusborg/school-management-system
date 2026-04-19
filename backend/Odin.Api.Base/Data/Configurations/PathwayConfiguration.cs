using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PathwayConfiguration : IEntityTypeConfiguration<Pathway>
{
    public void Configure(EntityTypeBuilder<Pathway> builder)
    {
        builder.HasKey(e => e.PathwayId);
        builder.Property(e => e.PathwayId).ValueGeneratedNever();
        builder.HasData(
            // Master / Doctor programmes
            new Pathway { PathwayId = 1,  Name = "Pathway 1: Direct Entry via Bachelor's Degree" },
            new Pathway { PathwayId = 2,  Name = "Pathway 2: Advanced Diploma + 3 Years Work Exp" },
            new Pathway { PathwayId = 3,  Name = "Pathway 3: Diploma + 5 Years Work Exp" },
            new Pathway { PathwayId = 4,  Name = "Pathway 4: High School + 8 Years Work Exp" },
            // Doctor only
            new Pathway { PathwayId = 5,  Name = "Pathway 1: Master's Degree (Preferred Entry)" },
            new Pathway { PathwayId = 6,  Name = "Pathway 2: Bachelor's Degree + 5 Years Work Exp" },
            new Pathway { PathwayId = 7,  Name = "Pathway 3: Advanced Diploma + 7 Years Work Exp" },
            new Pathway { PathwayId = 8,  Name = "Pathway 4: Diploma + 9 Years Work Exp" },
            new Pathway { PathwayId = 9,  Name = "Pathway 5: High School + 12 Years Work Exp" },
            // Diploma
            new Pathway { PathwayId = 10, Name = "Open Entry" },
            // Advanced Diploma
            new Pathway { PathwayId = 11, Name = "Pathway 1: Diploma or Associate Degree" },
            new Pathway { PathwayId = 12, Name = "Pathway 2: High School Certificate + 3 Years Work Exp" },
            // Bachelor
            new Pathway { PathwayId = 13, Name = "Pathway 1: Advanced Diploma" },
            new Pathway { PathwayId = 14, Name = "Pathway 2: Diploma + 2 Years Work Exp" },
            new Pathway { PathwayId = 15, Name = "Pathway 3: High School Certificate + 5 Years Work Exp" });
    }
}
