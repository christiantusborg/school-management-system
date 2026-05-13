using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerContractNoteConfiguration : IEntityTypeConfiguration<PartnerContractNote>
{
    public void Configure(EntityTypeBuilder<PartnerContractNote> builder)
    {
        builder.HasKey(e => e.PartnerContractNoteId);
        builder.Property(e => e.Body).IsRequired();
        builder.HasOne(e => e.Author)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
