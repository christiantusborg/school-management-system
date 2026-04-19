using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Odin.Api.Base.Crypto;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data;

public class OdinDbContext(DbContextOptions<OdinDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<SessionToken> SessionTokens => Set<SessionToken>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<OpaqueCredential> OpaqueCredentials => Set<OpaqueCredential>();
    public DbSet<KemKeyPair> KemKeyPairs => Set<KemKeyPair>();
    public DbSet<OpaqueRecoveryCode> OpaqueRecoveryCodes => Set<OpaqueRecoveryCode>();
    public DbSet<UserTwoFactorMethod> UserTwoFactorMethods => Set<UserTwoFactorMethod>();
    public DbSet<Fido2Credential> Fido2Credentials => Set<Fido2Credential>();
    public DbSet<UserPhone> UserPhones => Set<UserPhone>();
    public DbSet<UserContactEmail> UserContactEmails => Set<UserContactEmail>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    // Student domain
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<ProgrammeDocumentRequirement> ProgrammeDocumentRequirements => Set<ProgrammeDocumentRequirement>();
    public DbSet<EnrollmentDocument> EnrollmentDocuments => Set<EnrollmentDocument>();
    public DbSet<EnrollmentStatus> EnrollmentStatuses => Set<EnrollmentStatus>();
    public DbSet<TuitionFeeStatus> TuitionFeeStatuses => Set<TuitionFeeStatus>();
    public DbSet<ModeOfStudy> ModesOfStudy => Set<ModeOfStudy>();
    public DbSet<Pathway> Pathways => Set<Pathway>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<FinalProjectStatus> FinalProjectStatuses => Set<FinalProjectStatus>();
    public DbSet<FinalProject> FinalProjects => Set<FinalProject>();
    public DbSet<SubjectGrade> SubjectGrades => Set<SubjectGrade>();
    public DbSet<StudentNote> StudentNotes => Set<StudentNote>();
    public DbSet<PartnerProgrammeAccess> PartnerProgrammeAccesses => Set<PartnerProgrammeAccess>();
    public DbSet<PathwayDocumentRequirement> PathwayDocumentRequirements => Set<PathwayDocumentRequirement>();
    public DbSet<ProgrammePathway> ProgrammePathways => Set<ProgrammePathway>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OdinDbContext).Assembly);

        var byteArrayComparer = new ValueComparer<byte[]>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (h, b) => HashCode.Combine(h, b.GetHashCode())),
            v => v.ToArray());

        builder.Entity<OpaqueCredential>()
            .Property(e => e.OprfSeed)
            .HasConversion(
                v => FieldEncryption.Encrypt(v),
                v => FieldEncryption.Decrypt(v),
                byteArrayComparer);

        builder.Entity<OpaqueRecoveryCode>()
            .Property(e => e.OprfSeed)
            .HasConversion(
                v => FieldEncryption.Encrypt(v),
                v => FieldEncryption.Decrypt(v),
                byteArrayComparer);

        builder.Entity<UserTwoFactorMethod>()
            .Property(e => e.TotpSecret)
            .HasConversion(
                v => v == null ? null : FieldEncryption.EncryptString(v),
                v => v == null ? null : FieldEncryption.DecryptString(v));

        builder.Entity<InviteCode>()
            .Property(e => e.Code)
            .HasConversion(
                v => FieldEncryption.EncryptString(v),
                v => FieldEncryption.DecryptString(v));

    }
}
