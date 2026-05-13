using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdditionalEnrollmentStatues",
                columns: table => new
                {
                    AdditionalStatueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalEnrollmentStatues", x => x.AdditionalStatueId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RecoveryCodesConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeId);
                });

            migrationBuilder.CreateTable(
                name: "EducationLevels",
                columns: table => new
                {
                    EducationLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevels", x => x.EducationLevelId);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
                });

            migrationBuilder.CreateTable(
                name: "ModesOfStudy",
                columns: table => new
                {
                    ModeOfStudyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModesOfStudy", x => x.ModeOfStudyId);
                });

            migrationBuilder.CreateTable(
                name: "Nationalities",
                columns: table => new
                {
                    NationalityId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationalities", x => x.NationalityId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerAddressTypes",
                columns: table => new
                {
                    PartnerAddressTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAddressTypes", x => x.PartnerAddressTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "text", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.PartnerId);
                });

            migrationBuilder.CreateTable(
                name: "Pathways",
                columns: table => new
                {
                    PathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MinimumYearsWorkExperience = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pathways", x => x.PathwayId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fido2Credentials",
                columns: table => new
                {
                    Fido2CredentialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Transports = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fido2Credentials", x => x.Fido2CredentialId);
                    table.ForeignKey(
                        name: "FK_Fido2Credentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InviteCodes",
                columns: table => new
                {
                    InviteCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RedeemedByUserId = table.Column<string>(type: "text", nullable: true),
                    AssignedRole = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCodes", x => x.InviteCodeId);
                    table.ForeignKey(
                        name: "FK_InviteCodes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InviteCodes_AspNetUsers_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KemKeyPairs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedPrivateKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KemKeyPairs", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_KemKeyPairs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpaqueCredentials",
                columns: table => new
                {
                    OpaqueCredentialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OprfSeed = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientPublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpaqueCredentials", x => x.OpaqueCredentialId);
                    table.ForeignKey(
                        name: "FK_OpaqueCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpaqueRecoveryCodes",
                columns: table => new
                {
                    OpaqueRecoveryCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CodeId = table.Column<string>(type: "text", nullable: false),
                    OprfSeed = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClientPublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedPrivateKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpaqueRecoveryCodes", x => x.OpaqueRecoveryCodeId);
                    table.ForeignKey(
                        name: "FK_OpaqueRecoveryCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeviceInfo = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddresses",
                columns: table => new
                {
                    UserAddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddresses", x => x.UserAddressId);
                    table.ForeignKey(
                        name: "FK_UserAddresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserContactEmails",
                columns: table => new
                {
                    UserContactEmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContactEmails", x => x.UserContactEmailId);
                    table.ForeignKey(
                        name: "FK_UserContactEmails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPhones",
                columns: table => new
                {
                    UserPhoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPhones", x => x.UserPhoneId);
                    table.ForeignKey(
                        name: "FK_UserPhones_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserProfileId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorMethods",
                columns: table => new
                {
                    UserTwoFactorMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MethodType = table.Column<int>(type: "integer", nullable: false),
                    TotpSecret = table.Column<string>(type: "text", nullable: true),
                    LastTotpStepUsed = table.Column<long>(type: "bigint", nullable: true),
                    EnabledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactorMethods", x => x.UserTwoFactorMethodId);
                    table.ForeignKey(
                        name: "FK_UserTwoFactorMethods_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    StudentNumber = table.Column<string>(type: "text", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportId = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    HighestDegree = table.Column<string>(type: "text", nullable: true),
                    LanguageResult = table.Column<string>(type: "text", nullable: true),
                    YearsWorkExperience = table.Column<int>(type: "integer", nullable: false),
                    WizardStep = table.Column<int>(type: "integer", nullable: false),
                    NationalityId = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Students_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "NationalityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartnerAddresses",
                columns: table => new
                {
                    PartnerAddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerAddressTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Line1 = table.Column<string>(type: "text", nullable: true),
                    Line2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    StateRegion = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAddresses", x => x.PartnerAddressId);
                    table.ForeignKey(
                        name: "FK_PartnerAddresses_PartnerAddressTypes_PartnerAddressTypeId",
                        column: x => x.PartnerAddressTypeId,
                        principalTable: "PartnerAddressTypes",
                        principalColumn: "PartnerAddressTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerAddresses_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContactEmails",
                columns: table => new
                {
                    PartnerContactEmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContactEmails", x => x.PartnerContactEmailId);
                    table.ForeignKey(
                        name: "FK_PartnerContactEmails_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContactPhones",
                columns: table => new
                {
                    PartnerContactPhoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContactPhones", x => x.PartnerContactPhoneId);
                    table.ForeignKey(
                        name: "FK_PartnerContactPhones_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContracts",
                columns: table => new
                {
                    PartnerContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContracts", x => x.PartnerContractId);
                    table.ForeignKey(
                        name: "FK_PartnerContracts_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerUsers",
                columns: table => new
                {
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerUsers", x => new { x.PartnerId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PartnerUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerUsers_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Programmes",
                columns: table => new
                {
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmes", x => x.ProgrammeId);
                    table.ForeignKey(
                        name: "FK_Programmes_Partners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PathwayAcceptedEducationLevels",
                columns: table => new
                {
                    PathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    EducationLevelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayAcceptedEducationLevels", x => new { x.PathwayId, x.EducationLevelId });
                    table.ForeignKey(
                        name: "FK_PathwayAcceptedEducationLevels_EducationLevels_EducationLev~",
                        column: x => x.EducationLevelId,
                        principalTable: "EducationLevels",
                        principalColumn: "EducationLevelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathwayAcceptedEducationLevels_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PathwayDocumentRequirements",
                columns: table => new
                {
                    PathwayDocumentRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PathwayId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayDocumentRequirements", x => x.PathwayDocumentRequirementId);
                    table.ForeignKey(
                        name: "FK_PathwayDocumentRequirements_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PathwayDocumentRequirements_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathwayDocumentRequirements_Pathways_PathwayId1",
                        column: x => x.PathwayId1,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId");
                });

            migrationBuilder.CreateTable(
                name: "StudentDocuments",
                columns: table => new
                {
                    StudentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VerifiedByPartnerAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VerifiedByEnrolmentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocuments", x => x.StudentDocumentId);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentLanguages",
                columns: table => new
                {
                    StudentLanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<int>(type: "integer", nullable: false),
                    Proficiency = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLanguages", x => x.StudentLanguageId);
                    table.ForeignKey(
                        name: "FK_StudentLanguages_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentLanguages_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContractNotes",
                columns: table => new
                {
                    PartnerContractNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContractNotes", x => x.PartnerContractNoteId);
                    table.ForeignKey(
                        name: "FK_PartnerContractNotes_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerContractNotes_PartnerContracts_PartnerContractId",
                        column: x => x.PartnerContractId,
                        principalTable: "PartnerContracts",
                        principalColumn: "PartnerContractId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammePartners",
                columns: table => new
                {
                    ProgrammePartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammePartners", x => x.ProgrammePartnerId);
                    table.ForeignKey(
                        name: "FK_ProgrammePartners_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgrammePartners_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammePathways",
                columns: table => new
                {
                    ProgrammePathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammePathways", x => x.ProgrammePathwayId);
                    table.ForeignKey(
                        name: "FK_ProgrammePathways_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammePathways_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Specializations",
                columns: table => new
                {
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DurationOfStudyMonths = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specializations", x => x.SpecializationId);
                    table.ForeignKey(
                        name: "FK_Specializations_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommencementDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApplicationSubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ApplicationReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApplicationOfferAcceptedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdmissionConfirmedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GradesSubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GraduatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GraduationApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdditionalStatueId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeOfStudyId = table.Column<int>(type: "integer", nullable: false),
                    PathwayId = table.Column<int>(type: "integer", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.StudentEnrollmentId);
                    table.ForeignKey(
                        name: "FK_Enrollments_AdditionalEnrollmentStatues_AdditionalStatueId",
                        column: x => x.AdditionalStatueId,
                        principalTable: "AdditionalEnrollmentStatues",
                        principalColumn: "AdditionalStatueId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_ModesOfStudy_ModeOfStudyId",
                        column: x => x.ModeOfStudyId,
                        principalTable: "ModesOfStudy",
                        principalColumn: "ModeOfStudyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "SpecializationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecializationModesOfStudy",
                columns: table => new
                {
                    SpecializationModeOfStudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeOfStudyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializationModesOfStudy", x => x.SpecializationModeOfStudyId);
                    table.ForeignKey(
                        name: "FK_SpecializationModesOfStudy_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "SpecializationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Ects = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                    table.ForeignKey(
                        name: "FK_Subjects_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "SpecializationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentPayments",
                columns: table => new
                {
                    StudentEnrollmentPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaymentDueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDateAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPayments", x => x.StudentEnrollmentPaymentId);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Enrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentNotes",
                columns: table => new
                {
                    StudentNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentNotes", x => x.StudentNoteId);
                    table.ForeignKey(
                        name: "FK_StudentNotes_Enrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentNotes_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubjectGrades",
                columns: table => new
                {
                    SubjectGradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGrades", x => x.SubjectGradeId);
                    table.ForeignKey(
                        name: "FK_SubjectGrades_Enrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectGrades_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageId", "Code", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, "ab", null, "Abkhazian" },
                    { 2, "aa", null, "Afar" },
                    { 3, "af", null, "Afrikaans" },
                    { 4, "ak", null, "Akan" },
                    { 5, "sq", null, "Albanian" },
                    { 6, "am", null, "Amharic" },
                    { 7, "ar", null, "Arabic" },
                    { 8, "an", null, "Aragonese" },
                    { 9, "hy", null, "Armenian" },
                    { 10, "as", null, "Assamese" },
                    { 11, "av", null, "Avaric" },
                    { 12, "ae", null, "Avestan" },
                    { 13, "ay", null, "Aymara" },
                    { 14, "az", null, "Azerbaijani" },
                    { 15, "bm", null, "Bambara" },
                    { 16, "ba", null, "Bashkir" },
                    { 17, "eu", null, "Basque" },
                    { 18, "be", null, "Belarusian" },
                    { 19, "bn", null, "Bengali" },
                    { 20, "bi", null, "Bislama" },
                    { 21, "bs", null, "Bosnian" },
                    { 22, "br", null, "Breton" },
                    { 23, "bg", null, "Bulgarian" },
                    { 24, "my", null, "Burmese" },
                    { 25, "ca", null, "Catalan" },
                    { 26, "ch", null, "Chamorro" },
                    { 27, "ce", null, "Chechen" },
                    { 28, "ny", null, "Chichewa" },
                    { 29, "zh", null, "Chinese" },
                    { 30, "cu", null, "Church Slavonic" },
                    { 31, "cv", null, "Chuvash" },
                    { 32, "kw", null, "Cornish" },
                    { 33, "co", null, "Corsican" },
                    { 34, "cr", null, "Cree" },
                    { 35, "hr", null, "Croatian" },
                    { 36, "cs", null, "Czech" },
                    { 37, "da", null, "Danish" },
                    { 38, "dv", null, "Divehi" },
                    { 39, "nl", null, "Dutch" },
                    { 40, "dz", null, "Dzongkha" },
                    { 41, "en", null, "English" },
                    { 42, "eo", null, "Esperanto" },
                    { 43, "et", null, "Estonian" },
                    { 44, "ee", null, "Ewe" },
                    { 45, "fo", null, "Faroese" },
                    { 46, "fj", null, "Fijian" },
                    { 47, "fi", null, "Finnish" },
                    { 48, "fr", null, "French" },
                    { 49, "fy", null, "Western Frisian" },
                    { 50, "ff", null, "Fulah" },
                    { 51, "gd", null, "Scottish Gaelic" },
                    { 52, "gl", null, "Galician" },
                    { 53, "lg", null, "Ganda" },
                    { 54, "ka", null, "Georgian" },
                    { 55, "de", null, "German" },
                    { 56, "el", null, "Greek" },
                    { 57, "kl", null, "Kalaallisut" },
                    { 58, "gn", null, "Guarani" },
                    { 59, "gu", null, "Gujarati" },
                    { 60, "ht", null, "Haitian Creole" },
                    { 61, "ha", null, "Hausa" },
                    { 62, "he", null, "Hebrew" },
                    { 63, "hz", null, "Herero" },
                    { 64, "hi", null, "Hindi" },
                    { 65, "ho", null, "Hiri Motu" },
                    { 66, "hu", null, "Hungarian" },
                    { 67, "is", null, "Icelandic" },
                    { 68, "io", null, "Ido" },
                    { 69, "ig", null, "Igbo" },
                    { 70, "id", null, "Indonesian" },
                    { 71, "ia", null, "Interlingua" },
                    { 72, "ie", null, "Interlingue" },
                    { 73, "iu", null, "Inuktitut" },
                    { 74, "ik", null, "Inupiaq" },
                    { 75, "ga", null, "Irish" },
                    { 76, "it", null, "Italian" },
                    { 77, "ja", null, "Japanese" },
                    { 78, "jv", null, "Javanese" },
                    { 79, "kn", null, "Kannada" },
                    { 80, "kr", null, "Kanuri" },
                    { 81, "ks", null, "Kashmiri" },
                    { 82, "kk", null, "Kazakh" },
                    { 83, "km", null, "Khmer" },
                    { 84, "ki", null, "Kikuyu" },
                    { 85, "rw", null, "Kinyarwanda" },
                    { 86, "ky", null, "Kyrgyz" },
                    { 87, "kv", null, "Komi" },
                    { 88, "kg", null, "Kongo" },
                    { 89, "ko", null, "Korean" },
                    { 90, "kj", null, "Kuanyama" },
                    { 91, "ku", null, "Kurdish" },
                    { 92, "lo", null, "Lao" },
                    { 93, "la", null, "Latin" },
                    { 94, "lv", null, "Latvian" },
                    { 95, "li", null, "Limburgish" },
                    { 96, "ln", null, "Lingala" },
                    { 97, "lt", null, "Lithuanian" },
                    { 98, "lu", null, "Luba-Katanga" },
                    { 99, "lb", null, "Luxembourgish" },
                    { 100, "mk", null, "Macedonian" },
                    { 101, "mg", null, "Malagasy" },
                    { 102, "ms", null, "Malay" },
                    { 103, "ml", null, "Malayalam" },
                    { 104, "mt", null, "Maltese" },
                    { 105, "gv", null, "Manx" },
                    { 106, "mi", null, "Maori" },
                    { 107, "mr", null, "Marathi" },
                    { 108, "mh", null, "Marshallese" },
                    { 109, "mn", null, "Mongolian" },
                    { 110, "na", null, "Nauru" },
                    { 111, "nv", null, "Navajo" },
                    { 112, "nd", null, "North Ndebele" },
                    { 113, "nr", null, "South Ndebele" },
                    { 114, "ng", null, "Ndonga" },
                    { 115, "ne", null, "Nepali" },
                    { 116, "no", null, "Norwegian" },
                    { 117, "nb", null, "Norwegian Bokmål" },
                    { 118, "nn", null, "Norwegian Nynorsk" },
                    { 119, "ii", null, "Sichuan Yi" },
                    { 120, "oc", null, "Occitan" },
                    { 121, "oj", null, "Ojibwa" },
                    { 122, "or", null, "Oriya" },
                    { 123, "om", null, "Oromo" },
                    { 124, "os", null, "Ossetian" },
                    { 125, "pi", null, "Pali" },
                    { 126, "ps", null, "Pashto" },
                    { 127, "fa", null, "Persian" },
                    { 128, "pl", null, "Polish" },
                    { 129, "pt", null, "Portuguese" },
                    { 130, "pa", null, "Punjabi" },
                    { 131, "qu", null, "Quechua" },
                    { 132, "ro", null, "Romanian" },
                    { 133, "rm", null, "Romansh" },
                    { 134, "rn", null, "Rundi" },
                    { 135, "ru", null, "Russian" },
                    { 136, "se", null, "Northern Sami" },
                    { 137, "sm", null, "Samoan" },
                    { 138, "sg", null, "Sango" },
                    { 139, "sa", null, "Sanskrit" },
                    { 140, "sc", null, "Sardinian" },
                    { 141, "sr", null, "Serbian" },
                    { 142, "sn", null, "Shona" },
                    { 143, "sd", null, "Sindhi" },
                    { 144, "si", null, "Sinhala" },
                    { 145, "sk", null, "Slovak" },
                    { 146, "sl", null, "Slovenian" },
                    { 147, "so", null, "Somali" },
                    { 148, "st", null, "Southern Sotho" },
                    { 149, "es", null, "Spanish" },
                    { 150, "su", null, "Sundanese" },
                    { 151, "sw", null, "Swahili" },
                    { 152, "ss", null, "Swati" },
                    { 153, "sv", null, "Swedish" },
                    { 154, "tl", null, "Tagalog" },
                    { 155, "ty", null, "Tahitian" },
                    { 156, "tg", null, "Tajik" },
                    { 157, "ta", null, "Tamil" },
                    { 158, "tt", null, "Tatar" },
                    { 159, "te", null, "Telugu" },
                    { 160, "th", null, "Thai" },
                    { 161, "bo", null, "Tibetan" },
                    { 162, "ti", null, "Tigrinya" },
                    { 163, "to", null, "Tongan" },
                    { 164, "ts", null, "Tsonga" },
                    { 165, "tn", null, "Tswana" },
                    { 166, "tr", null, "Turkish" },
                    { 167, "tk", null, "Turkmen" },
                    { 168, "tw", null, "Twi" },
                    { 169, "ug", null, "Uyghur" },
                    { 170, "uk", null, "Ukrainian" },
                    { 171, "ur", null, "Urdu" },
                    { 172, "uz", null, "Uzbek" },
                    { 173, "ve", null, "Venda" },
                    { 174, "vi", null, "Vietnamese" },
                    { 175, "vo", null, "Volapük" },
                    { 176, "wa", null, "Walloon" },
                    { 177, "cy", null, "Welsh" },
                    { 178, "wo", null, "Wolof" },
                    { 179, "xh", null, "Xhosa" },
                    { 180, "yi", null, "Yiddish" },
                    { 181, "yo", null, "Yoruba" },
                    { 182, "za", null, "Zhuang" },
                    { 183, "zu", null, "Zulu" }
                });

            migrationBuilder.InsertData(
                table: "ModesOfStudy",
                columns: new[] { "ModeOfStudyId", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, null, "Distance/Online Self-Study" },
                    { 2, null, "Blended Learning" },
                    { 3, null, "Full-Time On-Campus" }
                });

            migrationBuilder.InsertData(
                table: "Nationalities",
                columns: new[] { "NationalityId", "Code", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, "AF", null, "Afghanistan" },
                    { 2, "AX", null, "Åland Islands" },
                    { 3, "AL", null, "Albania" },
                    { 4, "DZ", null, "Algeria" },
                    { 5, "AS", null, "American Samoa" },
                    { 6, "AD", null, "Andorra" },
                    { 7, "AO", null, "Angola" },
                    { 8, "AI", null, "Anguilla" },
                    { 9, "AQ", null, "Antarctica" },
                    { 10, "AG", null, "Antigua and Barbuda" },
                    { 11, "AR", null, "Argentina" },
                    { 12, "AM", null, "Armenia" },
                    { 13, "AW", null, "Aruba" },
                    { 14, "AU", null, "Australia" },
                    { 15, "AT", null, "Austria" },
                    { 16, "AZ", null, "Azerbaijan" },
                    { 17, "BS", null, "Bahamas" },
                    { 18, "BH", null, "Bahrain" },
                    { 19, "BD", null, "Bangladesh" },
                    { 20, "BB", null, "Barbados" },
                    { 21, "BY", null, "Belarus" },
                    { 22, "BE", null, "Belgium" },
                    { 23, "BZ", null, "Belize" },
                    { 24, "BJ", null, "Benin" },
                    { 25, "BM", null, "Bermuda" },
                    { 26, "BT", null, "Bhutan" },
                    { 27, "BO", null, "Bolivia" },
                    { 28, "BQ", null, "Bonaire, Sint Eustatius and Saba" },
                    { 29, "BA", null, "Bosnia and Herzegovina" },
                    { 30, "BW", null, "Botswana" },
                    { 31, "BV", null, "Bouvet Island" },
                    { 32, "BR", null, "Brazil" },
                    { 33, "IO", null, "British Indian Ocean Territory" },
                    { 34, "BN", null, "Brunei" },
                    { 35, "BG", null, "Bulgaria" },
                    { 36, "BF", null, "Burkina Faso" },
                    { 37, "BI", null, "Burundi" },
                    { 38, "CV", null, "Cabo Verde" },
                    { 39, "KH", null, "Cambodia" },
                    { 40, "CM", null, "Cameroon" },
                    { 41, "CA", null, "Canada" },
                    { 42, "KY", null, "Cayman Islands" },
                    { 43, "CF", null, "Central African Republic" },
                    { 44, "TD", null, "Chad" },
                    { 45, "CL", null, "Chile" },
                    { 46, "CN", null, "China" },
                    { 47, "CX", null, "Christmas Island" },
                    { 48, "CC", null, "Cocos (Keeling) Islands" },
                    { 49, "CO", null, "Colombia" },
                    { 50, "KM", null, "Comoros" },
                    { 51, "CG", null, "Congo" },
                    { 52, "CD", null, "Congo (DRC)" },
                    { 53, "CK", null, "Cook Islands" },
                    { 54, "CR", null, "Costa Rica" },
                    { 55, "CI", null, "Côte d'Ivoire" },
                    { 56, "HR", null, "Croatia" },
                    { 57, "CU", null, "Cuba" },
                    { 58, "CW", null, "Curaçao" },
                    { 59, "CY", null, "Cyprus" },
                    { 60, "CZ", null, "Czechia" },
                    { 61, "DK", null, "Denmark" },
                    { 62, "DJ", null, "Djibouti" },
                    { 63, "DM", null, "Dominica" },
                    { 64, "DO", null, "Dominican Republic" },
                    { 65, "EC", null, "Ecuador" },
                    { 66, "EG", null, "Egypt" },
                    { 67, "SV", null, "El Salvador" },
                    { 68, "GQ", null, "Equatorial Guinea" },
                    { 69, "ER", null, "Eritrea" },
                    { 70, "EE", null, "Estonia" },
                    { 71, "SZ", null, "Eswatini" },
                    { 72, "ET", null, "Ethiopia" },
                    { 73, "FK", null, "Falkland Islands" },
                    { 74, "FO", null, "Faroe Islands" },
                    { 75, "FJ", null, "Fiji" },
                    { 76, "FI", null, "Finland" },
                    { 77, "FR", null, "France" },
                    { 78, "GF", null, "French Guiana" },
                    { 79, "PF", null, "French Polynesia" },
                    { 80, "TF", null, "French Southern Territories" },
                    { 81, "GA", null, "Gabon" },
                    { 82, "GM", null, "Gambia" },
                    { 83, "GE", null, "Georgia" },
                    { 84, "DE", null, "Germany" },
                    { 85, "GH", null, "Ghana" },
                    { 86, "GI", null, "Gibraltar" },
                    { 87, "GR", null, "Greece" },
                    { 88, "GL", null, "Greenland" },
                    { 89, "GD", null, "Grenada" },
                    { 90, "GP", null, "Guadeloupe" },
                    { 91, "GU", null, "Guam" },
                    { 92, "GT", null, "Guatemala" },
                    { 93, "GG", null, "Guernsey" },
                    { 94, "GN", null, "Guinea" },
                    { 95, "GW", null, "Guinea-Bissau" },
                    { 96, "GY", null, "Guyana" },
                    { 97, "HT", null, "Haiti" },
                    { 98, "HM", null, "Heard Island and McDonald Islands" },
                    { 99, "VA", null, "Holy See" },
                    { 100, "HN", null, "Honduras" },
                    { 101, "HK", null, "Hong Kong" },
                    { 102, "HU", null, "Hungary" },
                    { 103, "IS", null, "Iceland" },
                    { 104, "IN", null, "India" },
                    { 105, "ID", null, "Indonesia" },
                    { 106, "IR", null, "Iran" },
                    { 107, "IQ", null, "Iraq" },
                    { 108, "IE", null, "Ireland" },
                    { 109, "IM", null, "Isle of Man" },
                    { 110, "IL", null, "Israel" },
                    { 111, "IT", null, "Italy" },
                    { 112, "JM", null, "Jamaica" },
                    { 113, "JP", null, "Japan" },
                    { 114, "JE", null, "Jersey" },
                    { 115, "JO", null, "Jordan" },
                    { 116, "KZ", null, "Kazakhstan" },
                    { 117, "KE", null, "Kenya" },
                    { 118, "KI", null, "Kiribati" },
                    { 119, "KP", null, "North Korea" },
                    { 120, "KR", null, "South Korea" },
                    { 121, "KW", null, "Kuwait" },
                    { 122, "KG", null, "Kyrgyzstan" },
                    { 123, "LA", null, "Laos" },
                    { 124, "LV", null, "Latvia" },
                    { 125, "LB", null, "Lebanon" },
                    { 126, "LS", null, "Lesotho" },
                    { 127, "LR", null, "Liberia" },
                    { 128, "LY", null, "Libya" },
                    { 129, "LI", null, "Liechtenstein" },
                    { 130, "LT", null, "Lithuania" },
                    { 131, "LU", null, "Luxembourg" },
                    { 132, "MO", null, "Macao" },
                    { 133, "MG", null, "Madagascar" },
                    { 134, "MW", null, "Malawi" },
                    { 135, "MY", null, "Malaysia" },
                    { 136, "MV", null, "Maldives" },
                    { 137, "ML", null, "Mali" },
                    { 138, "MT", null, "Malta" },
                    { 139, "MH", null, "Marshall Islands" },
                    { 140, "MQ", null, "Martinique" },
                    { 141, "MR", null, "Mauritania" },
                    { 142, "MU", null, "Mauritius" },
                    { 143, "YT", null, "Mayotte" },
                    { 144, "MX", null, "Mexico" },
                    { 145, "FM", null, "Micronesia" },
                    { 146, "MD", null, "Moldova" },
                    { 147, "MC", null, "Monaco" },
                    { 148, "MN", null, "Mongolia" },
                    { 149, "ME", null, "Montenegro" },
                    { 150, "MS", null, "Montserrat" },
                    { 151, "MA", null, "Morocco" },
                    { 152, "MZ", null, "Mozambique" },
                    { 153, "MM", null, "Myanmar" },
                    { 154, "NA", null, "Namibia" },
                    { 155, "NR", null, "Nauru" },
                    { 156, "NP", null, "Nepal" },
                    { 157, "NL", null, "Netherlands" },
                    { 158, "NC", null, "New Caledonia" },
                    { 159, "NZ", null, "New Zealand" },
                    { 160, "NI", null, "Nicaragua" },
                    { 161, "NE", null, "Niger" },
                    { 162, "NG", null, "Nigeria" },
                    { 163, "NU", null, "Niue" },
                    { 164, "NF", null, "Norfolk Island" },
                    { 165, "MK", null, "North Macedonia" },
                    { 166, "MP", null, "Northern Mariana Islands" },
                    { 167, "NO", null, "Norway" },
                    { 168, "OM", null, "Oman" },
                    { 169, "PK", null, "Pakistan" },
                    { 170, "PW", null, "Palau" },
                    { 171, "PS", null, "Palestine" },
                    { 172, "PA", null, "Panama" },
                    { 173, "PG", null, "Papua New Guinea" },
                    { 174, "PY", null, "Paraguay" },
                    { 175, "PE", null, "Peru" },
                    { 176, "PH", null, "Philippines" },
                    { 177, "PN", null, "Pitcairn" },
                    { 178, "PL", null, "Poland" },
                    { 179, "PT", null, "Portugal" },
                    { 180, "PR", null, "Puerto Rico" },
                    { 181, "QA", null, "Qatar" },
                    { 182, "RE", null, "Réunion" },
                    { 183, "RO", null, "Romania" },
                    { 184, "RU", null, "Russia" },
                    { 185, "RW", null, "Rwanda" },
                    { 186, "BL", null, "Saint Barthélemy" },
                    { 187, "SH", null, "Saint Helena" },
                    { 188, "KN", null, "Saint Kitts and Nevis" },
                    { 189, "LC", null, "Saint Lucia" },
                    { 190, "MF", null, "Saint Martin (French part)" },
                    { 191, "PM", null, "Saint Pierre and Miquelon" },
                    { 192, "VC", null, "Saint Vincent and the Grenadines" },
                    { 193, "WS", null, "Samoa" },
                    { 194, "SM", null, "San Marino" },
                    { 195, "ST", null, "Sao Tome and Principe" },
                    { 196, "SA", null, "Saudi Arabia" },
                    { 197, "SN", null, "Senegal" },
                    { 198, "RS", null, "Serbia" },
                    { 199, "SC", null, "Seychelles" },
                    { 200, "SL", null, "Sierra Leone" },
                    { 201, "SG", null, "Singapore" },
                    { 202, "SX", null, "Sint Maarten (Dutch part)" },
                    { 203, "SK", null, "Slovakia" },
                    { 204, "SI", null, "Slovenia" },
                    { 205, "SB", null, "Solomon Islands" },
                    { 206, "SO", null, "Somalia" },
                    { 207, "ZA", null, "South Africa" },
                    { 208, "GS", null, "South Georgia and the South Sandwich Islands" },
                    { 209, "SS", null, "South Sudan" },
                    { 210, "ES", null, "Spain" },
                    { 211, "LK", null, "Sri Lanka" },
                    { 212, "SD", null, "Sudan" },
                    { 213, "SR", null, "Suriname" },
                    { 214, "SJ", null, "Svalbard and Jan Mayen" },
                    { 215, "SE", null, "Sweden" },
                    { 216, "CH", null, "Switzerland" },
                    { 217, "SY", null, "Syria" },
                    { 218, "TW", null, "Taiwan" },
                    { 219, "TJ", null, "Tajikistan" },
                    { 220, "TZ", null, "Tanzania" },
                    { 221, "TH", null, "Thailand" },
                    { 222, "TL", null, "Timor-Leste" },
                    { 223, "TG", null, "Togo" },
                    { 224, "TK", null, "Tokelau" },
                    { 225, "TO", null, "Tonga" },
                    { 226, "TT", null, "Trinidad and Tobago" },
                    { 227, "TN", null, "Tunisia" },
                    { 228, "TR", null, "Türkiye" },
                    { 229, "TM", null, "Turkmenistan" },
                    { 230, "TC", null, "Turks and Caicos Islands" },
                    { 231, "TV", null, "Tuvalu" },
                    { 232, "UG", null, "Uganda" },
                    { 233, "UA", null, "Ukraine" },
                    { 234, "AE", null, "United Arab Emirates" },
                    { 235, "GB", null, "United Kingdom" },
                    { 236, "US", null, "United States" },
                    { 237, "UM", null, "United States Minor Outlying Islands" },
                    { 238, "UY", null, "Uruguay" },
                    { 239, "UZ", null, "Uzbekistan" },
                    { 240, "VU", null, "Vanuatu" },
                    { 241, "VE", null, "Venezuela" },
                    { 242, "VN", null, "Vietnam" },
                    { 243, "VG", null, "Virgin Islands (British)" },
                    { 244, "VI", null, "Virgin Islands (U.S.)" },
                    { 245, "WF", null, "Wallis and Futuna" },
                    { 246, "EH", null, "Western Sahara" },
                    { 247, "YE", null, "Yemen" },
                    { 248, "ZM", null, "Zambia" },
                    { 249, "ZW", null, "Zimbabwe" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_StudentEnrollmentId",
                table: "EnrollmentPayments",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_AdditionalStatueId",
                table: "Enrollments",
                column: "AdditionalStatueId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ModeOfStudyId",
                table: "Enrollments",
                column: "ModeOfStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PartnerId",
                table: "Enrollments",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SpecializationId",
                table: "Enrollments",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Fido2Credentials_UserId",
                table: "Fido2Credentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_CreatedByUserId",
                table: "InviteCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_RedeemedByUserId",
                table: "InviteCodes",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Code",
                table: "Languages",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nationalities_Code",
                table: "Nationalities",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OpaqueCredentials_UserId",
                table: "OpaqueCredentials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpaqueRecoveryCodes_UserId",
                table: "OpaqueRecoveryCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAddresses_PartnerAddressTypeId",
                table: "PartnerAddresses",
                column: "PartnerAddressTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAddresses_PartnerId",
                table: "PartnerAddresses",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAddressTypes_Code",
                table: "PartnerAddressTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContactEmails_PartnerId",
                table: "PartnerContactEmails",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContactPhones_PartnerId",
                table: "PartnerContactPhones",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContractNotes_AuthorUserId",
                table: "PartnerContractNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContractNotes_PartnerContractId",
                table: "PartnerContractNotes",
                column: "PartnerContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContracts_PartnerId",
                table: "PartnerContracts",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Slug",
                table: "Partners",
                column: "Slug",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerUsers_UserId",
                table: "PartnerUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayAcceptedEducationLevels_EducationLevelId",
                table: "PathwayAcceptedEducationLevels",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayDocumentRequirements_DocumentTypeId",
                table: "PathwayDocumentRequirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayDocumentRequirements_PathwayId_DocumentTypeId",
                table: "PathwayDocumentRequirements",
                columns: new[] { "PathwayId", "DocumentTypeId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayDocumentRequirements_PathwayId1",
                table: "PathwayDocumentRequirements",
                column: "PathwayId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePartners_PartnerId",
                table: "ProgrammePartners",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePartners_ProgrammeId_PartnerId",
                table: "ProgrammePartners",
                columns: new[] { "ProgrammeId", "PartnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePathways_PathwayId",
                table: "ProgrammePathways",
                column: "PathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePathways_ProgrammeId_PathwayId",
                table: "ProgrammePathways",
                columns: new[] { "ProgrammeId", "PathwayId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_Code",
                table: "Programmes",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_OwnerId",
                table: "Programmes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTokens_TokenHash",
                table: "SessionTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTokens_UserId",
                table: "SessionTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializationModesOfStudy_SpecializationId_ModeOfStudyId",
                table: "SpecializationModesOfStudy",
                columns: new[] { "SpecializationId", "ModeOfStudyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specializations_ProgrammeId_Code",
                table: "Specializations",
                columns: new[] { "ProgrammeId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_StudentId",
                table: "StudentDocuments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLanguages_LanguageId",
                table: "StudentLanguages",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLanguages_StudentId_LanguageId",
                table: "StudentLanguages",
                columns: new[] { "StudentId", "LanguageId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_StudentEnrollmentId",
                table: "StudentNotes",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_StudentId",
                table: "StudentNotes",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NationalityId",
                table: "Students",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGrades_StudentEnrollmentId",
                table: "SubjectGrades",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGrades_SubjectId",
                table: "SubjectGrades",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SpecializationId",
                table: "Subjects",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_UserId",
                table: "UserAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContactEmails_UserId",
                table: "UserContactEmails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_UserId",
                table: "UserPhones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorMethods_UserId",
                table: "UserTwoFactorMethods",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EnrollmentPayments");

            migrationBuilder.DropTable(
                name: "Fido2Credentials");

            migrationBuilder.DropTable(
                name: "InviteCodes");

            migrationBuilder.DropTable(
                name: "KemKeyPairs");

            migrationBuilder.DropTable(
                name: "OpaqueCredentials");

            migrationBuilder.DropTable(
                name: "OpaqueRecoveryCodes");

            migrationBuilder.DropTable(
                name: "PartnerAddresses");

            migrationBuilder.DropTable(
                name: "PartnerContactEmails");

            migrationBuilder.DropTable(
                name: "PartnerContactPhones");

            migrationBuilder.DropTable(
                name: "PartnerContractNotes");

            migrationBuilder.DropTable(
                name: "PartnerUsers");

            migrationBuilder.DropTable(
                name: "PathwayAcceptedEducationLevels");

            migrationBuilder.DropTable(
                name: "PathwayDocumentRequirements");

            migrationBuilder.DropTable(
                name: "ProgrammePartners");

            migrationBuilder.DropTable(
                name: "ProgrammePathways");

            migrationBuilder.DropTable(
                name: "SessionTokens");

            migrationBuilder.DropTable(
                name: "SpecializationModesOfStudy");

            migrationBuilder.DropTable(
                name: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "StudentLanguages");

            migrationBuilder.DropTable(
                name: "StudentNotes");

            migrationBuilder.DropTable(
                name: "SubjectGrades");

            migrationBuilder.DropTable(
                name: "UserAddresses");

            migrationBuilder.DropTable(
                name: "UserContactEmails");

            migrationBuilder.DropTable(
                name: "UserPhones");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserTwoFactorMethods");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "PartnerAddressTypes");

            migrationBuilder.DropTable(
                name: "PartnerContracts");

            migrationBuilder.DropTable(
                name: "EducationLevels");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "Pathways");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "AdditionalEnrollmentStatues");

            migrationBuilder.DropTable(
                name: "ModesOfStudy");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Specializations");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Nationalities");

            migrationBuilder.DropTable(
                name: "Programmes");

            migrationBuilder.DropTable(
                name: "Partners");
        }
    }
}
