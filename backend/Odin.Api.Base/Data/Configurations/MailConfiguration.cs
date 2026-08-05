using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Mail;

namespace Odin.Api.Base.Data.Configurations;

public class MailAccountConfiguration : IEntityTypeConfiguration<MailAccount>
{
    public void Configure(EntityTypeBuilder<MailAccount> builder)
    {
        builder.HasKey(e => e.MailAccountId);
        builder.Property(e => e.DisplayName).HasMaxLength(200);
        builder.Property(e => e.EmailAddress).HasMaxLength(320);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.Property(e => e.ImapHost).HasMaxLength(300);
        builder.Property(e => e.SmtpHost).HasMaxLength(300);
        builder.Property(e => e.Username).HasMaxLength(320);
        builder.Property(e => e.LastSyncError).HasMaxLength(2000);
    }
}

public class MailAccountAccessConfiguration : IEntityTypeConfiguration<MailAccountAccess>
{
    public void Configure(EntityTypeBuilder<MailAccountAccess> builder)
    {
        builder.HasKey(e => e.MailAccountAccessId);
        builder.HasIndex(e => new { e.MailAccountId, e.UserId }).IsUnique();
        builder.HasOne(e => e.Account)
            .WithMany(a => a.Access)
            .HasForeignKey(e => e.MailAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MailFolderConfiguration : IEntityTypeConfiguration<MailFolder>
{
    public void Configure(EntityTypeBuilder<MailFolder> builder)
    {
        builder.HasKey(e => e.MailFolderId);
        builder.Property(e => e.Name).HasMaxLength(500);
        builder.HasIndex(e => new { e.MailAccountId, e.Name }).IsUnique();
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.MailAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MailMessageConfiguration : IEntityTypeConfiguration<MailMessage>
{
    public void Configure(EntityTypeBuilder<MailMessage> builder)
    {
        builder.HasKey(e => e.MailMessageId);
        builder.Property(e => e.MessageIdHeader).HasMaxLength(500);
        builder.Property(e => e.InReplyTo).HasMaxLength(500);
        builder.Property(e => e.Subject).HasMaxLength(1000);
        builder.Property(e => e.FromAddress).HasMaxLength(320);
        builder.Property(e => e.FromName).HasMaxLength(300);
        builder.Property(e => e.ToAddresses).HasMaxLength(4000);
        builder.Property(e => e.CcAddresses).HasMaxLength(4000);
        builder.HasIndex(e => new { e.MailFolderId, e.ImapUid });
        builder.HasIndex(e => e.MailAccountId);
        builder.HasIndex(e => e.FromAddress);
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.MailAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Folder)
            .WithMany()
            .HasForeignKey(e => e.MailFolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MailAttachmentConfiguration : IEntityTypeConfiguration<MailAttachment>
{
    public void Configure(EntityTypeBuilder<MailAttachment> builder)
    {
        builder.HasKey(e => e.MailAttachmentId);
        builder.Property(e => e.FileName).HasMaxLength(500);
        builder.Property(e => e.ContentType).HasMaxLength(200);
        builder.HasOne(e => e.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(e => e.MailMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MailMessageLinkConfiguration : IEntityTypeConfiguration<MailMessageLink>
{
    public void Configure(EntityTypeBuilder<MailMessageLink> builder)
    {
        builder.HasKey(e => e.MailMessageLinkId);
        builder.Property(e => e.MatchedAddress).HasMaxLength(320);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.CrmLeadId);
        builder.HasIndex(e => e.PartnerId);
        builder.HasOne(e => e.Message)
            .WithMany(m => m.Links)
            .HasForeignKey(e => e.MailMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
