using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.HasKey(e => e.DocumentTypeId);
        builder.Property(e => e.DocumentTypeId).ValueGeneratedNever();
        builder.HasData(
            new DocumentType { DocumentTypeId = 1, Name = "Passport" },
            new DocumentType { DocumentTypeId = 2, Name = "CV" },
            new DocumentType { DocumentTypeId = 3, Name = "Degree Certificate" },
            new DocumentType { DocumentTypeId = 4, Name = "Language Certificate" },
            new DocumentType { DocumentTypeId = 5, Name = "Other" });
    }
}
