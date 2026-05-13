using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Odin.Api.Base.Crypto;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Partners;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data;

public class OdinDbContext(DbContextOptions<OdinDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    // ── Identity / auth / opaque ──────────────────────────────────────────
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

    // ── Partner ───────────────────────────────────────────────────────────
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerAddress> PartnerAddresses => Set<PartnerAddress>();
    public DbSet<PartnerAddressType> PartnerAddressTypes => Set<PartnerAddressType>();
    public DbSet<PartnerContactEmail> PartnerContactEmails => Set<PartnerContactEmail>();
    public DbSet<PartnerContactPhone> PartnerContactPhones => Set<PartnerContactPhone>();
    public DbSet<PartnerContract> PartnerContracts => Set<PartnerContract>();
    public DbSet<PartnerContractNote> PartnerContractNotes => Set<PartnerContractNote>();
    public DbSet<PartnerUsers> PartnerUsers => Set<PartnerUsers>();

    // ── Programme / Specialization / Subject ──────────────────────────────
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ProgrammePartner> ProgrammePartners => Set<ProgrammePartner>();
    public DbSet<ProgrammePathway> ProgrammePathways => Set<ProgrammePathway>();
    public DbSet<ProgrammeDocumentRequirement> ProgrammeDocumentRequirements => Set<ProgrammeDocumentRequirement>();
    public DbSet<PartnerProgrammeStatus> PartnerProgrammeStatuses => Set<PartnerProgrammeStatus>();
    public DbSet<SpecializationModeOfStudy> SpecializationModesOfStudy => Set<SpecializationModeOfStudy>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();
    public DbSet<LetterAsset> LetterAssets => Set<LetterAsset>();

    // ── Pathway ───────────────────────────────────────────────────────────
    public DbSet<Pathway> Pathways => Set<Pathway>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<PathwayAcceptedEducationLevel> PathwayAcceptedEducationLevels => Set<PathwayAcceptedEducationLevel>();
    public DbSet<PathwayDocumentRequirement> PathwayDocumentRequirements => Set<PathwayDocumentRequirement>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentTypeVerifyRequirement> DocumentTypeVerifyRequirements => Set<DocumentTypeVerifyRequirement>();

    // ── Enrollment ────────────────────────────────────────────────────────
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<EnrollmentStatusNote> EnrollmentStatusNotes => Set<EnrollmentStatusNote>();
    public DbSet<EnrollmentStatus> EnrollmentStatuses => Set<EnrollmentStatus>();
    public DbSet<ModeOfStudy> ModesOfStudy => Set<ModeOfStudy>();
    public DbSet<EnrollmentPayment> EnrollmentPayments => Set<EnrollmentPayment>();
    public DbSet<SubjectGrade> SubjectGrades => Set<SubjectGrade>();

    // ── Student ───────────────────────────────────────────────────────────
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StudentDocumentNote> StudentDocumentNotes => Set<StudentDocumentNote>();
    public DbSet<DocumentStatus> DocumentStatuses => Set<DocumentStatus>();
    public DbSet<StudentNote> StudentNotes => Set<StudentNote>();
    public DbSet<UserLanguage> UserLanguages => Set<UserLanguage>();

    // ── Reference ─────────────────────────────────────────────────────────
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();


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
