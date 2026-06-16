using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odin.Api.Base.Crypto;
using Odin.Api.Base.Email;

namespace Odin.Api.Base.Data.Configurations;

public class MailSettingsConfiguration : IEntityTypeConfiguration<MailSettings>
{
    public void Configure(EntityTypeBuilder<MailSettings> builder)
    {
        builder.HasKey(e => e.MailSettingsId);

        builder.Property(e => e.Provider).HasMaxLength(32).IsRequired();
        builder.Property(e => e.GmailImpersonatedUser).HasMaxLength(256);
        builder.Property(e => e.FromEmail).HasMaxLength(256);
        builder.Property(e => e.FromName).HasMaxLength(256);
        builder.Property(e => e.SmtpHost).HasMaxLength(256);
        builder.Property(e => e.SmtpUsername).HasMaxLength(256);
        builder.Property(e => e.SmtpSecurity).HasMaxLength(32);

        // Secrets: AES-256-GCM at rest, same scheme as OprfSeed / TotpSecret.
        builder.Property(e => e.GmailServiceAccountJson)
            .HasConversion(
                v => v == null ? null : FieldEncryption.EncryptString(v),
                v => v == null ? null : FieldEncryption.DecryptString(v))
            .HasColumnType("text");
        builder.Property(e => e.SmtpPassword)
            .HasConversion(
                v => v == null ? null : FieldEncryption.EncryptString(v),
                v => v == null ? null : FieldEncryption.DecryptString(v))
            .HasColumnType("text");
    }
}
