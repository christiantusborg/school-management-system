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
    public DbSet<School> Schools => Set<School>();
    public DbSet<PartnerDocumentType> PartnerDocumentTypes => Set<PartnerDocumentType>();
    public DbSet<PartnerDocument> PartnerDocuments => Set<PartnerDocument>();
    public DbSet<PartnerDatasheetDefinition> PartnerDatasheetDefinitions => Set<PartnerDatasheetDefinition>();
    public DbSet<PartnerDatasheetSection> PartnerDatasheetSections => Set<PartnerDatasheetSection>();
    public DbSet<PartnerDatasheetField> PartnerDatasheetFields => Set<PartnerDatasheetField>();
    public DbSet<PartnerDatasheet> PartnerDatasheets => Set<PartnerDatasheet>();
    public DbSet<PartnerDatasheetRow> PartnerDatasheetRows => Set<PartnerDatasheetRow>();
    public DbSet<PartnerDatasheetValue> PartnerDatasheetValues => Set<PartnerDatasheetValue>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Assignments.AssignmentUpload> AssignmentUploads => Set<SharedLibrary.Basics.Opaque.Domains.Assignments.AssignmentUpload>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Assignments.AssignmentComment> AssignmentComments => Set<SharedLibrary.Basics.Opaque.Domains.Assignments.AssignmentComment>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Payments.EnrollmentPaymentPlan> EnrollmentPaymentPlans => Set<SharedLibrary.Basics.Opaque.Domains.Payments.EnrollmentPaymentPlan>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Payments.PaymentInstallment> PaymentInstallments => Set<SharedLibrary.Basics.Opaque.Domains.Payments.PaymentInstallment>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Payments.AdditionalInvoice> AdditionalInvoices => Set<SharedLibrary.Basics.Opaque.Domains.Payments.AdditionalInvoice>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Payments.Currency> Currencies => Set<SharedLibrary.Basics.Opaque.Domains.Payments.Currency>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ProgrammePartner> ProgrammePartners => Set<ProgrammePartner>();
    public DbSet<ProgrammePathway> ProgrammePathways => Set<ProgrammePathway>();
    public DbSet<ProgrammeDocumentRequirement> ProgrammeDocumentRequirements => Set<ProgrammeDocumentRequirement>();
    public DbSet<PartnerProgrammeStatus> PartnerProgrammeStatuses => Set<PartnerProgrammeStatus>();
    public DbSet<SpecializationModeOfStudy> SpecializationModesOfStudy => Set<SpecializationModeOfStudy>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();
    public DbSet<LetterEmailTemplate> LetterEmailTemplates => Set<LetterEmailTemplate>();
    public DbSet<Odin.Api.Base.Email.MailSettings> MailSettings => Set<Odin.Api.Base.Email.MailSettings>();
    public DbSet<Odin.Api.Base.Email.SchoolMailSettings> SchoolMailSettings => Set<Odin.Api.Base.Email.SchoolMailSettings>();

    // ── Intake / questionnaire builder (ported from QuVian core) ──────────
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.QuestionnaireTemplate> QuestionnaireTemplates
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.QuestionnaireTemplate>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeInstance> IntakeInstances
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeInstance>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeResponse> IntakeResponses
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeResponse>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.QuestionnaireTemplateVersion> QuestionnaireTemplateVersions
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.QuestionnaireTemplateVersion>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeAssignment> IntakeAssignments
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeAssignment>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.FieldLibraryEntry> FieldLibraryEntries
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.FieldLibraryEntry>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.TextTemplate> IntakeTextTemplates
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.TextTemplate>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.GenerationRule> GenerationRules
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.GenerationRule>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplate> IntakeDocumentTemplates
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplate>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplateAsset> IntakeDocumentTemplateAssets
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplateAsset>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplateImage> IntakeDocumentTemplateImages
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.DocumentTemplateImage>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeOutput> IntakeOutputs
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.IntakeOutput>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.PublicForm> PublicForms
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.PublicForm>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.PublicFormSubmission> PublicFormSubmissions
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.PublicFormSubmission>();
    public DbSet<SharedLibrary.Basics.Opaque.Domains.Intake.PublicFormPayment> PublicFormPayments
        => Set<SharedLibrary.Basics.Opaque.Domains.Intake.PublicFormPayment>();
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
    public DbSet<EnrollmentModuleStart> EnrollmentModuleStarts => Set<EnrollmentModuleStart>();
    public DbSet<EnrollmentStatusNote> EnrollmentStatusNotes => Set<EnrollmentStatusNote>();
    public DbSet<EnrollmentStatus> EnrollmentStatuses => Set<EnrollmentStatus>();
    public DbSet<ModeOfStudy> ModesOfStudy => Set<ModeOfStudy>();
    public DbSet<EnrollmentPayment> EnrollmentPayments => Set<EnrollmentPayment>();
    public DbSet<SubjectGrade> SubjectGrades => Set<SubjectGrade>();

    // ── Student ───────────────────────────────────────────────────────────
    public DbSet<Student> Students => Set<Student>();
    public DbSet<PositionFunction> PositionFunctions => Set<PositionFunction>();
    public DbSet<EmploymentIndustry> EmploymentIndustries => Set<EmploymentIndustry>();
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
