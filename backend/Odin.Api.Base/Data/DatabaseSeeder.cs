using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Letters;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data;

public static class DatabaseSeeder
{
    private record SeedUser(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string OutputFile,
        string? AdminLevel = null);

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context     = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger      = scope.ServiceProvider.GetRequiredService<ILogger<OdinDbContext>>();

        await context.Database.MigrateAsync();

        // ── Roles ─────────────────────────────────────────────────────────────
        // Base roles plus the 5 admin privilege levels. Admin users hold the
        // generic `Admin` role (keeps existing AdminOnly endpoints working) AND
        // one of the 5 levels (used by the Admin Users page + SuperAdminOnly).
        var roles = new[] { "Admin", "Partner", "Student", "User" }.Concat(AdminLevels.All);
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }

        // ── Seed users ────────────────────────────────────────────────────────
        var seedUsers = new List<SeedUser>
        {
            new("admin", "admin@odin.local", "System", "Administrator", "Admin", "",         AdminLevels.Administrator),
            new("adm",   "adm@terbium.dk",   "Admin",  "User",          "Admin", "adm.txt",  AdminLevels.SuperAdministrator),
            new("ctu",   "ctu@terbium.dk",   "CTU",    "User",          "User",  "ctu.txt"),
            new("ict",   "ict@terbium.dk",   "ICT",    "User",          "User",  "ict.txt"),
        };

        foreach (var seed in seedUsers)
        {
            var existing = await userManager.FindByNameAsync(seed.Username);
            if (existing is not null)
            {
                // User exists — ensure the expected level role is attached.
                // This is idempotent: re-seeds backfill roles added since the
                // original seeding (e.g. when AdminLevels were introduced).
                if (seed.AdminLevel is not null && !await userManager.IsInRoleAsync(existing, seed.AdminLevel))
                {
                    logger.LogInformation("[Seeder] Assigning level '{Level}' to existing user '{Username}'",
                        seed.AdminLevel, seed.Username);
                    await userManager.AddToRoleAsync(existing, seed.AdminLevel);
                }
                else
                {
                    logger.LogInformation("[Seeder] User '{Username}' already exists, skipping", seed.Username);
                }
                continue;
            }

            var password = seed.Username is "admin" or "adm" ? "Admin@123!" : GeneratePassword();
            logger.LogInformation("[Seeder] Creating user '{Username}' with email '{Email}'", seed.Username, seed.Email);

            var credentials = await ComputeOpaqueCredentials(password, logger);

            var user = new ApplicationUser
            {
                UserName             = seed.Username,
                Email                = seed.Email,
                EmailConfirmed       = true,
                IsEnabled            = true,
                RecoveryCodesConfirmed = true
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                logger.LogError("[Seeder] Failed to create user '{Username}': {Errors}",
                    seed.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role);
            if (seed.AdminLevel is not null)
                await userManager.AddToRoleAsync(user, seed.AdminLevel);

            context.OpaqueCredentials.Add(new OpaqueCredential
            {
                UserId        = user.Id,
                OprfSeed      = credentials.OprfSeed,
                ClientPublicKey = credentials.ClientPublicKey
            });

            context.KemKeyPairs.Add(new KemKeyPair
            {
                UserId             = user.Id,
                PublicKey          = credentials.KemPublicKey,
                EncryptedPrivateKey = credentials.KemEncryptedPrivKey,
                Nonce              = credentials.KemNonce
            });

            context.UserProfiles.Add(new UserProfile
            {
                UserId    = user.Id,
                FirstName = seed.FirstName,
                LastName  = seed.LastName
            });

            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Created user '{Username}' (Id={Id})", seed.Username, user.Id);

            // Write credential file for non-admin seed users
            if (!string.IsNullOrEmpty(seed.OutputFile))
            {
                var outputDir  = Path.GetFullPath(AppContext.BaseDirectory);
                var outputPath = Path.Combine(outputDir, seed.OutputFile);
                await File.WriteAllTextAsync(outputPath,
                    $"""
                    Username : {seed.Username}
                    Email    : {seed.Email}
                    Password : {password}
                    Role     : {seed.Role}
                    UserId   : {user.Id}
                    """);
                logger.LogInformation("[Seeder] Credentials written to {Path}", outputPath);
            }
        }

        // Catalogue/pathway/partner seeding was removed when the domain model
        // refactor renamed/dropped the underlying entities (Major, EnrollmentStatus,
        // FinalProjectStatus, TuitionFeeStatus, the old Partner shape, etc.).
        // Re-seeding is now done by the user out-of-band; the admin users above
        // are sufficient to bring the system up.

        await SeedDocumentTypesAsync(context, logger);
        await SeedSystemDocumentTypesAsync(context, logger);
        await SeedDocumentTypeAiPromptsAsync(context, logger);
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        await SeedSystemLetterAssetsAsync(context, fileStorage, logger);
        await SeedDefaultAdmissionLetterAsync(context, logger);
        await SeedDefaultOfferLetterAsync(context, logger);
        await SeedDefaultTranscriptAsync(context, logger);
        await SeedDefaultCertificateAsync(context, logger);
        await SeedDefaultProvisionalCertificateAsync(context, logger);
        await SeedDefaultPrintableTranscriptAsync(context, logger);
        await SeedDefaultStudentIdCardAsync(context, logger);
        await SeedDefaultLetterLayoutsAsync(context, logger);
        var eduLevelByName = await SeedEducationLevelsAsync(context, logger);
        await SeedPositionFunctionsAsync(context, logger);
        await SeedEmploymentIndustriesAsync(context, logger);
        await DecryptLegacyIntakeAnswersAsync(context, logger);
        await SeedDefaultQuestionnairesAsync(context, logger);
        await SeedPartnerDocumentTypesAsync(context, logger);
        await SeedPartnerDatasheetTemplatesAsync(context, logger);
        await SeedFacultyProfileStructureAsync(context, logger);
        await SeedCohortTypesAsync(context, logger);
        await SeedCohortUploadFieldsAsync(context, logger);
        await SeedPathwaysAsync(context, logger, eduLevelByName);
        await SeedIbssCoreProgrammesAsync(context, logger);
        await SeedDemoPartnersAsync(context, logger);
        await SeedProgrammePathwayLinksAsync(context, logger);
        await SeedEnrollmentStatusesAsync(context, logger);
        await SeedDocumentStatusesAsync(context, logger);
        await SeedDocumentTypeVerifyRequirementsAsync(context, logger);
        await SeedProgrammeDocumentRequirementsAsync(context, logger);
    }

    /// <summary>
    /// Ensures every active <see cref="Programme"/> has a row in
    /// <see cref="ProgrammeDocumentRequirement"/> for each of the canonical
    /// 4 doc-types (resolved by Name). Idempotent: only inserts missing
    /// links, never deletes. Runs on every boot so a freshly-added programme
    /// picks up the default requirement set without manual SQL.
    /// </summary>
    private static async Task SeedProgrammeDocumentRequirementsAsync(
        OdinDbContext context, ILogger logger)
    {
        var canonicalNames = new[]
        {
            "Passport",
            "Bachelor's Degree Certificate",
            "Language Proficiency Certificate",
            "Curriculum Vitae",
        };

        var docTypeIds = await context.DocumentTypes
            .Where(t => t.DeletedAt == null && canonicalNames.Contains(t.Name))
            .Select(t => t.DocumentTypeId)
            .ToListAsync();
        if (docTypeIds.Count == 0) return;

        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var existing = await context.ProgrammeDocumentRequirements
            .Where(r => r.DeletedAt == null)
            .Select(r => new { r.ProgrammeId, r.DocumentTypeId })
            .ToListAsync();
        var existingSet = existing
            .Select(r => (r.ProgrammeId, r.DocumentTypeId))
            .ToHashSet();

        var added = 0;
        foreach (var pid in programmeIds)
        foreach (var dtid in docTypeIds)
        {
            if (existingSet.Contains((pid, dtid))) continue;
            context.ProgrammeDocumentRequirements.Add(new ProgrammeDocumentRequirement
            {
                ProgrammeDocumentRequirementId = Guid.NewGuid(),
                ProgrammeId = pid,
                DocumentTypeId = dtid,
            });
            added++;
        }
        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] ProgrammeDocumentRequirements: +{Count} added", added);
        }
    }

    /// <summary>
    /// Seeds default verify-requirement checklists for every existing
    /// <see cref="DocumentType"/>. Idempotent on (DocumentTypeId, Name).
    /// Pattern matching on the type name picks the appropriate checklist
    /// so future doc types added in <see cref="SeedDocumentTypesAsync"/>
    /// pick up requirements automatically on next boot.
    /// </summary>
    private static async Task SeedDocumentTypeVerifyRequirementsAsync(
        OdinDbContext context, ILogger logger)
    {
        // (Name, RejectionLabel) pairs — positive checklist phrasing first,
        // negative reject-chip phrasing second. RejectionLabel may be null
        // (frontend falls back to Name), kept non-null here for clarity.
        var identity = new (string Name, string Reject)[]
        {
            ("Photo is legible",            "Photo unclear"),
            ("Name matches form",           "Name mismatch with form"),
            ("Date of birth matches form",  "DOB mismatch with form"),
            ("Document not expired",        "ID expired"),
            ("Scan not cut off",            "Scan cut off"),
        };
        var photo = new (string, string)[]
        {
            ("Plain background",            "Background not plain"),
            ("Recent (≤ 6 months)",         "Photo not recent"),
            ("Face clearly visible",        "Face not clearly visible"),
        };
        var certificate = new (string, string)[]
        {
            ("Document is readable",        "Unreadable"),
            ("Issuing institution recognized", "Institution not accredited"),
            ("Award date matches form",     "Date inconsistent"),
            ("Stamp / signature present",   "Missing stamp/signature"),
        };
        var transcript = new (string, string)[]
        {
            ("Document is readable",        "Unreadable"),
            ("Grade scale declared",        "Grade scale missing"),
            ("All years included",          "Years missing"),
            ("Stamp / signature present",   "Missing stamp/signature"),
        };
        var cv = new (string, string)[]
        {
            ("Sufficient experience for pathway", "Insufficient experience"),
            ("Gaps explained",                    "Gaps unexplained"),
            ("Consistent with declared data",     "Inconsistent with declared data"),
        };
        var language = new (string, string)[]
        {
            ("Score meets minimum",         "Score below minimum"),
            ("Test still valid (≤ 2 yrs)",  "Test expired (>2 yrs)"),
            ("Certificate type accepted",   "Wrong certificate type"),
        };
        var generic = new (string, string)[]
        {
            ("Document is readable",        "Unreadable"),
            ("Author / source identified",  "Author/source unclear"),
            ("Relevant to application",     "Not relevant"),
        };

        (string, string)[] Pick(string name)
        {
            var n = name.ToLowerInvariant();
            if (n.Contains("passport photograph")) return photo;
            if (n.Contains("passport") || n.Contains("national id")
                || n.Contains("birth certificate")
                || n.Contains("visa") || n.Contains("residence permit"))
                return identity;
            if (n.Contains("language proficiency")) return language;
            if (n.Contains("curriculum vitae") || n == "cv" || n.Contains("résumé") || n.Contains("resume"))
                return cv;
            if (n.EndsWith("transcript")) return transcript;
            if (n.EndsWith("certificate")) return certificate;
            return generic;
        }

        var docTypes = await context.DocumentTypes
            .Where(t => t.DeletedAt == null)
            .Select(t => new { t.DocumentTypeId, t.Name })
            .ToListAsync();
        var existing = await context.DocumentTypeVerifyRequirements
            .Where(r => r.DeletedAt == null)
            .Select(r => new { r.DocumentTypeId, r.Name })
            .ToListAsync();
        var existingSet = existing
            .Select(r => (r.DocumentTypeId, Name: r.Name.Trim().ToLowerInvariant()))
            .ToHashSet();

        var added = 0;
        foreach (var dt in docTypes)
        {
            foreach (var (req, reject) in Pick(dt.Name))
            {
                var key = (dt.DocumentTypeId, req.Trim().ToLowerInvariant());
                if (existingSet.Contains(key)) continue;
                context.DocumentTypeVerifyRequirements.Add(new DocumentTypeVerifyRequirement
                {
                    DocumentTypeVerifyRequirementId = Guid.NewGuid(),
                    DocumentTypeId = dt.DocumentTypeId,
                    Name = req,
                    RejectionLabel = reject,
                });
                added++;
            }
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] DocumentTypeVerifyRequirements: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] DocumentTypeVerifyRequirements already complete — skipping");
        }
    }

    private static async Task SeedDocumentStatusesAsync(OdinDbContext context, ILogger logger)
    {
        var existing = await context.DocumentStatuses.Select(s => s.DocumentStatusId).ToListAsync();
        var existingSet = existing.ToHashSet();
        var added = 0;
        foreach (var seed in DocumentStatusIds.All)
        {
            if (existingSet.Contains(seed.Id)) continue;
            context.DocumentStatuses.Add(new DocumentStatus
            {
                DocumentStatusId = seed.Id,
                Code = seed.Code,
                Name = seed.Name,
            });
            added++;
        }
        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] DocumentStatuses: +{Count} added", added);
        }
    }

    private static async Task SeedEnrollmentStatusesAsync(OdinDbContext context, ILogger logger)
    {
        var existing = await context.EnrollmentStatuses.ToListAsync();
        var byId = existing.ToDictionary(s => s.EnrollmentStatusId);
        var added = 0;
        var updated = 0;
        foreach (var seed in EnrollmentStatusIds.All)
        {
            if (byId.TryGetValue(seed.Id, out var row))
            {
                // Re-apply canonical text/flow on every boot so renames + level
                // bumps in the seed data don't drift from the row in DB.
                var changed = row.Code != seed.Code
                    || row.Name != seed.Name
                    || row.Level != seed.Level
                    || row.LevelDown != seed.LevelDown
                    || row.NextActionRole != seed.NextActionRole
                    || row.NextStatusOnCompleteId != seed.NextStatusOnCompleteId;
                if (!changed) continue;
                row.Code = seed.Code;
                row.Name = seed.Name;
                row.Level = seed.Level;
                row.LevelDown = seed.LevelDown;
                row.NextActionRole = seed.NextActionRole;
                row.NextStatusOnCompleteId = seed.NextStatusOnCompleteId;
                updated++;
            }
            else
            {
                context.EnrollmentStatuses.Add(new EnrollmentStatus
                {
                    EnrollmentStatusId = seed.Id,
                    Code = seed.Code,
                    Name = seed.Name,
                    Level = seed.Level,
                    LevelDown = seed.LevelDown,
                    NextActionRole = seed.NextActionRole,
                    NextStatusOnCompleteId = seed.NextStatusOnCompleteId,
                });
                added++;
            }
        }
        if (added > 0 || updated > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] EnrollmentStatuses: +{Added} added, ~{Updated} updated", added, updated);
        }
    }

    /// <summary>
    /// Wires the IBSS core programmes to the entry pathways the wizard
    /// surfaces. Mapping is by name prefix:
    ///   • BBA  ← every "Bachelor Top-Up" pathway
    ///   • MBA  ← every "Master" pathway
    ///   • DBA  ← every "Doctor" pathway
    ///   • EDBA ← every "Doctor" pathway
    /// Idempotent on (ProgrammeId, PathwayId).
    /// </summary>
    private static async Task SeedProgrammePathwayLinksAsync(OdinDbContext context, ILogger logger)
    {
        var programmes = await context.Programmes
            .Where(p => p.DeletedAt == null && p.OwnerId == null)
            .Select(p => new { p.ProgrammeId, p.Code })
            .ToListAsync();
        if (programmes.Count == 0) return;

        var pathways = await context.Pathways
            .Where(p => p.DeletedAt == null)
            .Select(p => new { p.PathwayId, p.Name })
            .ToListAsync();
        if (pathways.Count == 0) return;

        Guid[] PathwaysByPrefix(string prefix) =>
            pathways.Where(p => p.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.PathwayId).ToArray();

        var bachelorTopUp = PathwaysByPrefix("Bachelor Top-Up");
        var master        = PathwaysByPrefix("Master");
        var doctor        = PathwaysByPrefix("Doctor");

        var wiring = new List<(Guid ProgrammeId, Guid PathwayId)>();
        foreach (var p in programmes)
        {
            var pids = p.Code.ToUpperInvariant() switch
            {
                "BBA"  => bachelorTopUp,
                "MBA"  => master,
                "DBA"  => doctor,
                "EDBA" => doctor,
                _      => Array.Empty<Guid>(),
            };
            foreach (var pid in pids) wiring.Add((p.ProgrammeId, pid));
        }
        if (wiring.Count == 0) return;

        var existing = await context.ProgrammePathways
            .Select(pp => new { pp.ProgrammeId, pp.PathwayId })
            .ToListAsync();
        var existingSet = existing
            .Select(e => (e.ProgrammeId, e.PathwayId))
            .ToHashSet();

        var added = 0;
        foreach (var (programmeId, pathwayId) in wiring)
        {
            if (existingSet.Contains((programmeId, pathwayId))) continue;
            context.ProgrammePathways.Add(new ProgrammePathway
            {
                ProgrammePathwayId = Guid.NewGuid(),
                ProgrammeId = programmeId,
                PathwayId = pathwayId,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Programme↔Pathway links: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Programme↔Pathway links already complete — skipping");
        }
    }

    /// <summary>
    /// Seeds 2 demo partners so the public signup wizard
    /// (`/v1/public/partner/{slug}/catalogue`) has something to resolve in
    /// non-prod environments. Idempotent on Slug.
    /// </summary>
    private static async Task SeedDemoPartnersAsync(OdinDbContext context, ILogger logger)
    {
        var seed = new (string Slug, string Name, string Website)[]
        {
            ("curium-academy",       "Curium Academy",       "https://curium.dk"),
            ("demo-business-school", "Demo Business School", "https://demo.ibss.curium.dk"),
        };

        var existingSlugs = (await context.Partners.Select(p => p.Slug).ToListAsync())
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var added = 0;
        foreach (var (slug, name, website) in seed)
        {
            if (existingSlugs.Contains(slug.ToLowerInvariant())) continue;
            context.Partners.Add(new SharedLibrary.Basics.Opaque.Domains.Partners.Partner
            {
                PartnerId = Guid.NewGuid(),
                Slug = slug,
                Name = name,
                Website = website,
                PartnerNumber = $"PA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Demo partners: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Demo partners already present — skipping");
        }
    }

    /// <summary>
    /// Pre-seeds the 5 IBSS-branded image assets that the default admission
    /// letter template references. Files are bundled with the assembly under
    /// <c>Letters/SeedAssets/</c>; this routine copies them into
    /// <see cref="IFileStorage"/> exactly once per stable id and creates the
    /// matching <see cref="LetterAsset"/> rows. Idempotent on
    /// <c>LetterAssetId</c>.
    /// </summary>
    private static async Task SeedSystemLetterAssetsAsync(
        OdinDbContext context, IFileStorage storage, ILogger logger)
    {
        var seedDir = Path.Combine(AppContext.BaseDirectory, "Letters", "SeedAssets");
        if (!Directory.Exists(seedDir))
        {
            logger.LogWarning("[Seeder] Letters/SeedAssets directory missing at {Dir} — skipping asset seed", seedDir);
            return;
        }

        var existingIds = await context.LetterAssets
            .Select(a => a.LetterAssetId)
            .ToListAsync();
        var existingSet = existingIds.ToHashSet();

        var added = 0;
        foreach (var seed in SystemLetterAssetIds.All)
        {
            if (existingSet.Contains(seed.Id)) continue;

            var sourcePath = Path.Combine(seedDir, seed.ResourceFileName);
            if (!File.Exists(sourcePath))
            {
                logger.LogWarning("[Seeder] Seed asset file not found: {File}", sourcePath);
                continue;
            }

            string storagePath;
            await using (var fs = File.OpenRead(sourcePath))
            {
                storagePath = await storage.SaveAsync(
                    fs,
                    $"letter-assets/{seed.Id}-{seed.ResourceFileName}",
                    CancellationToken.None);
            }

            var size = new FileInfo(sourcePath).Length;
            context.LetterAssets.Add(new LetterAsset
            {
                LetterAssetId = seed.Id,
                Name = seed.Name,
                MimeType = seed.MimeType,
                StoragePath = storagePath,
                SizeBytes = size,
                UploadedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] System letter assets: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] System letter assets already complete — skipping");
        }
    }

    /// <summary>
    /// Every (programme, partner) pair that should own a letter template: each
    /// partner a shared core programme is granted to (ProgrammePartners), plus
    /// the owner of a partner-owned programme (Programme.OwnerId). Templates are
    /// per-partner, so the per-programme seeders fan out across these pairs.
    /// </summary>
    private static async Task<List<(Guid ProgrammeId, Guid PartnerId)>> LetterTemplatePairsAsync(OdinDbContext context)
    {
        var shared = await context.ProgrammePartners
            .Where(pp => pp.IsActive != null)
            .Select(pp => new { pp.ProgrammeId, pp.PartnerId })
            .ToListAsync();
        var owned = await context.Programmes
            .Where(p => p.DeletedAt == null && p.OwnerId != null)
            .Select(p => new { p.ProgrammeId, PartnerId = p.OwnerId!.Value })
            .ToListAsync();
        return shared.Concat(owned)
            .Select(x => (x.ProgrammeId, x.PartnerId))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Ensures an unpublished default template of <paramref name="type"/> exists
    /// for every (programme, partner) pair that lacks one. Idempotent on
    /// (ProgrammeId, PartnerId, LetterType); never overwrites an existing row.
    /// <paramref name="configure"/> fills the type-specific body/layout.
    /// </summary>
    private static async Task EnsureTemplatePerPairAsync(
        OdinDbContext context, ILogger logger, LetterType type, string label, Action<LetterTemplate> configure)
    {
        var pairs = await LetterTemplatePairsAsync(context);
        if (pairs.Count == 0) return;

        var have = (await context.LetterTemplates
            .Where(t => t.LetterType == type && t.DeletedAt == null)
            .Select(t => new { t.ProgrammeId, t.PartnerId })
            .ToListAsync())
            .Select(x => (x.ProgrammeId, x.PartnerId))
            .ToHashSet();

        var added = 0;
        foreach (var pair in pairs)
        {
            if (have.Contains(pair)) continue;
            var t = new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pair.ProgrammeId,
                PartnerId = pair.PartnerId,
                LetterType = type,
                UpdatedAt = DateTime.UtcNow,
            };
            configure(t);
            context.LetterTemplates.Add(t);
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] {Label}: +{Count} (programme,partner) pairs seeded", label, added);
        }
        else
        {
            logger.LogInformation("[Seeder] {Label}: every (programme,partner) pair already has one — skipping", label);
        }
    }

    /// <summary>
    /// Inserts the IBSS default <c>AdmissionLetter</c> body for every
    /// (programme, partner) pair that does not already have one. Idempotent.
    /// </summary>
    private static Task SeedDefaultAdmissionLetterAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.AdmissionLetter,
            "Default admission letter", t => t.BodyHtml = DefaultAdmissionLetterHtml);

    /// <summary>
    /// Inserts the IBSS default <c>OfferLetter</c> body for every
    /// (programme, partner) pair that does not already have one. Idempotent.
    /// </summary>
    private static Task SeedDefaultOfferLetterAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.OfferLetter,
            "Default offer letter", t => t.BodyHtml = DefaultOfferLetterHtml);

    /// <summary>
    /// Pre-fills <c>CertificateLayoutJson</c> with the canonical Konva layout
    /// for each text-heavy letter type (Offer / Admission / Transcript).
    /// Idempotent on (ProgrammeId, LetterType): only writes the layout when
    /// the row's CertificateLayoutJson is currently null/blank — admin edits
    /// to the JSON are never overwritten. The legacy BodyHtml stays in place
    /// as a fallback the renderer falls back to if a layout is missing.
    /// </summary>
    private static async Task SeedDefaultLetterLayoutsAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var seedByType = new Dictionary<LetterType, string>
        {
            [LetterType.OfferLetter]     = DefaultLetterLayouts.OfferLetterJson(),
            [LetterType.AdmissionLetter] = DefaultLetterLayouts.AdmissionLetterJson(),
            [LetterType.Transcript]      = DefaultLetterLayouts.TranscriptJson(),
        };

        var totalSet = 0;
        foreach (var (type, json) in seedByType)
        {
            // For Transcripts, also refresh templates whose stored layout has
            // an older fingerprint than the current seeded version — that's
            // how we ship structural updates (e.g. dynamic grades table) to
            // every programme without clobbering admin edits, since admin
            // saves don't change the fingerprint they originally read.
            var rows = await context.LetterTemplates
                .Where(t => t.LetterType == type
                    && programmeIds.Contains(t.ProgrammeId)
                    && t.DeletedAt == null)
                .ToListAsync();
            foreach (var row in rows)
            {
                if (row.CertificateLayoutJson is null)
                {
                    row.CertificateLayoutJson = json;
                    row.UpdatedAt = DateTime.UtcNow;
                    totalSet++;
                    continue;
                }
                if (type == LetterType.Transcript)
                {
                    // Only refresh layouts that still carry a seed fingerprint:
                    // those are untouched seeded defaults. The letter editor
                    // strips the fingerprint when an admin saves, so a missing
                    // fingerprint means the layout is admin-owned — it must
                    // survive every restart/redeploy untouched.
                    var existing = CertificateLayout.TryParse(row.CertificateLayoutJson);
                    var existingFp = existing?.SeedFingerprint ?? 0;
                    if (existingFp > 0 && existingFp < DefaultLetterLayouts.CurrentTranscriptFingerprint)
                    {
                        row.CertificateLayoutJson = json;
                        row.UpdatedAt = DateTime.UtcNow;
                        totalSet++;
                    }
                }
            }
        }

        if (totalSet > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default letter layouts: +{Count} programmes seeded across Offer/Admission/Transcript", totalSet);
        }
        else
        {
            logger.LogInformation("[Seeder] Default letter layouts already present — skipping");
        }
    }

    /// <summary>
    /// Inserts the IBSS default <c>Certificate</c> layout into every existing
    /// <see cref="Programme"/> that does not already have one. The layout
    /// references the pre-seeded background asset and contains the static
    /// "Hereby Awards To…" script text plus the four data fields.
    /// </summary>
    private static Task SeedDefaultCertificateAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.Certificate,
            "Default certificate layout", t => t.CertificateLayoutJson = DefaultCertificateLayoutJson);

    /// <summary>
    /// Seeds a starting layout for the Provisional Certificate (the
    /// stamp-and-signature-free variant). Reuses the standard certificate
    /// JSON so admins get the same field placement to start from; they swap
    /// the background image / remove signature fields per programme. Idempotent.
    /// </summary>
    private static Task SeedDefaultProvisionalCertificateAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.ProvisionalCertificate,
            "Default provisional certificate layout", t => t.CertificateLayoutJson = DefaultCertificateLayoutJson);

    /// <summary>
    /// Seeds a starting layout for the Printable Transcript (the Admission-only
    /// transcript variant). Reuses the standard Transcript layout so it renders
    /// the grade table out of the box, and publishes it so it releases at
    /// graduation alongside the digital transcript. Idempotent per (programme,
    /// partner) pair — admin edits are never overwritten.
    /// </summary>
    private static Task SeedDefaultPrintableTranscriptAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.PrintableTranscript,
            "Default printable transcript layout", t =>
            {
                t.CertificateLayoutJson = DefaultLetterLayouts.TranscriptJson();
                t.IsPublished = true;
            });

    /// <summary>
    /// Digital Student ID Card starter layout. Editable per (programme,
    /// partner) in the same editor as certificates; only reachable in the UI
    /// when the programme's IssueDigitalStudentCard toggle is on.
    /// </summary>
    private static async Task SeedDefaultStudentIdCardAsync(OdinDbContext context, ILogger logger)
    {
        // One-time refresh: card templates seeded with the original text-only
        // default (no background artwork) get upgraded to the designed layout.
        // Admin-edited layouts (which have a backgroundAssetId or changed
        // structure) are left alone.
        // CertificateLayoutJson is jsonb — Postgres has no LIKE for jsonb, so
        // the substring check must run in memory. Card templates are few.
        var cardTemplates = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.StudentIdCard && t.DeletedAt == null)
            .ToListAsync();
        var stale = cardTemplates
            .Where(t => t.CertificateLayoutJson != null
                && t.CertificateLayoutJson.Contains("STUDENT ID CARD")
                && !t.CertificateLayoutJson.Contains("backgroundAssetId"))
            .ToList();
        foreach (var t in stale)
        {
            t.CertificateLayoutJson = DefaultStudentIdCardLayoutJson;
            t.UpdatedAt = DateTime.UtcNow;
        }
        if (stale.Count > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Student ID card: upgraded {Count} templates to the designed card layout", stale.Count);
        }

        var missingPhoto = cardTemplates.Where(t => t.CertificateLayoutJson != null
            && t.CertificateLayoutJson.Contains(SystemLetterAssetIds.StudentCardBg.ToString())
            && !t.CertificateLayoutJson.Contains(SystemLetterAssetIds.StudentPhoto.ToString())).ToList();
        foreach (var t in missingPhoto)
        {
            t.CertificateLayoutJson = t.CertificateLayoutJson!.Replace("\"fields\": [",
                "\"fields\": [{\"id\": \"photo\", \"kind\": \"image\", \"imageAssetId\": \""
                + SystemLetterAssetIds.StudentPhoto + "\", \"x\": 40, \"y\": 270, \"width\": 250, \"height\": 350}, ");
            t.UpdatedAt = DateTime.UtcNow;
        }
        if (missingPhoto.Count > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Student ID card: added student-photo field to {Count} layouts", missingPhoto.Count);
        }

        await EnsureTemplatePerPairAsync(context, logger, LetterType.StudentIdCard,
            "Default student ID card layout", t =>
            {
                t.CertificateLayoutJson = DefaultStudentIdCardLayoutJson;
                t.IsPublished = true;
            });
    }

    /// <summary>
    /// Designed card layout: the seeded background PNG carries all static
    /// artwork (title, amber bands, labels, photo/QR/signature placeholders,
    /// terms); these fields overlay only the per-student values.
    /// </summary>
    private static string DefaultStudentIdCardLayoutJson { get; } = $@"{{
  ""backgroundAssetId"": ""{SystemLetterAssetIds.StudentCardBg}"",
  ""width"": 1000,
  ""height"": 1414,
  ""pageSize"": ""A4"",
  ""orientation"": ""portrait"",
  ""fields"": [
    {{ ""id"": ""photo"",  ""kind"": ""image"", ""imageAssetId"": ""{SystemLetterAssetIds.StudentPhoto}"", ""x"": 40, ""y"": 270, ""width"": 250, ""height"": 350 }},
    {{ ""id"": ""name"",   ""tag"": ""[student full name]"",  ""x"": 640, ""y"": 263,  ""fontSize"": 28, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""id"",     ""tag"": ""[student number]"",     ""x"": 640, ""y"": 345,  ""fontSize"": 28, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""intake"", ""tag"": ""[commencement date]"",  ""x"": 640, ""y"": 427,  ""fontSize"": 28, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""prog"",   ""tag"": ""[program name]"",       ""x"": 640, ""y"": 509,  ""fontSize"": 28, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""center"", ""tag"": ""[partner name]"",       ""x"": 640, ""y"": 591,  ""fontSize"": 28, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""dob"",    ""tag"": ""[date of birth]"",      ""x"": 600, ""y"": 827,  ""fontSize"": 26, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""tel"",    ""tag"": ""[student phone]"",      ""x"": 600, ""y"": 905,  ""fontSize"": 26, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""issue"",  ""tag"": ""[date]"",               ""x"": 600, ""y"": 983,  ""fontSize"": 26, ""color"": ""#4D3D99"" }},
    {{ ""id"": ""expiry"", ""tag"": ""[completion date]"",    ""x"": 600, ""y"": 1061, ""fontSize"": 26, ""color"": ""#4D3D99"" }}
  ]
}}";

    private static string DefaultCertificateLayoutJson { get; } = $@"{{
  ""backgroundAssetId"": ""{SystemLetterAssetIds.IbssCertificateBg}"",
  ""width"": 2000,
  ""height"": 1414,
  ""fields"": [
    {{ ""id"": ""studentId"",      ""tag"": ""[student number]"",      ""prefix"": ""Student ID: "", ""x"": 1900, ""y"": 130, ""fontSize"": 28, ""color"": ""#000000"", ""align"": ""right"",  ""bold"": true }},
    {{ ""id"": ""awardsTo"",       ""text"": ""My Global World Education Group Hereby Awards To"", ""x"": 0, ""y"": 360, ""fontSize"": 28, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""studentName"",    ""tag"": ""[student full name]"",   ""x"": 0, ""y"": 430, ""fontSize"": 52, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""whoSatisfied"",   ""text"": ""Who has satisfactorily completed the studies prescribed and therefore has been granted the degree of"", ""x"": 0, ""y"": 530, ""fontSize"": 24, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""programmeName"",  ""tag"": ""[program name]"",        ""x"": 0, ""y"": 600, ""fontSize"": 38, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""withSpec"",       ""text"": ""With a specialisation in"", ""x"": 0, ""y"": 690, ""fontSize"": 24, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""specName"",       ""tag"": ""[specialization name]"", ""x"": 0, ""y"": 760, ""fontSize"": 34, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""witnessLine1"",   ""text"": ""With all its right and privileges in the witness whereof the seal of the"", ""x"": 0, ""y"": 850, ""fontSize"": 22, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""witnessLine2"",   ""text"": ""My Global World Education Group is hereunto affixed."", ""x"": 0, ""y"": 890, ""fontSize"": 22, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""presentedOn"",    ""tag"": ""[graduation date]"",     ""prefix"": ""Presented on "", ""suffix"": "" in Denmark."", ""x"": 0, ""y"": 950, ""fontSize"": 24, ""color"": ""#000000"", ""align"": ""center"", ""bold"": true }}
  ]
}}";

    /// <summary>
    /// Inserts the IBSS default <c>Transcript</c> body into every existing
    /// <see cref="Programme"/> that does not already have one. Idempotent.
    /// </summary>
    private static Task SeedDefaultTranscriptAsync(OdinDbContext context, ILogger logger)
        => EnsureTemplatePerPairAsync(context, logger, LetterType.Transcript,
            "Default transcript", t => t.BodyHtml = DefaultTranscriptHtml);

    private static string DefaultTranscriptHtml { get; } = $@"
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""MGW"" /></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSecondaryLogo}"" alt="""" /></p>
<h1 style=""text-align:center;"">STUDENT TRANSCRIPT</h1>
<h3 style=""text-align:center;"">Official Transcript</h3>
<p>Date of issuance: <strong>[date]</strong></p>
<table>
<tbody>
<tr><td><strong>Student's Name</strong></td><td>:</td><td>[student full name]</td>
    <td><strong>Language of Instruction</strong></td><td>:</td><td>[instruction language]</td></tr>
<tr><td><strong>Student's ID Number</strong></td><td>:</td><td>[student number]</td>
    <td><strong>DOB</strong></td><td>:</td><td>[date of birth]</td></tr>
<tr><td><strong>Program of Study</strong></td><td>:</td><td>[program name]</td>
    <td><strong>ECTS Achieved</strong></td><td>:</td><td>[ects achieved]</td></tr>
<tr><td><strong>Specialization in</strong></td><td>:</td><td>[specialization name]</td>
    <td><strong>Graduation date</strong></td><td>:</td><td>[graduation date]</td></tr>
</tbody>
</table>
<p></p>
[transcript]
<p></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSignatureLine}"" alt=""Signature Line"" /></p>
<p><strong>Signature of School Official</strong></p>
<p>Anna Phan</p>
<p><strong>Official's Title:</strong> Head of Administration</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssStamp}"" alt=""Stamp"" /></p>
<h3>Grade Standard</h3>
<table>
<thead>
<tr><th>MGW Grade</th><th>UK Grade</th><th>US Grade</th><th>ECTS Grade</th><th>ECTS Grade Points</th><th>Remark</th></tr>
</thead>
<tbody>
<tr><td>75-100</td><td>75-100</td><td>A+</td><td>A</td><td>5.0</td><td>Excellent – outstanding performance with only minor errors</td></tr>
<tr><td>70-74</td><td>70-74</td><td>A</td><td>A</td><td>5.0</td><td>Excellent – outstanding performance with only minor errors</td></tr>
<tr><td>65-69</td><td>65-69</td><td>A-</td><td>B</td><td>4.0</td><td>Very good – above the average standard but with some errors</td></tr>
<tr><td>60-64</td><td>60-64</td><td>B+</td><td>C</td><td>3.0</td><td>Good – generally sound work with a number of notable errors</td></tr>
<tr><td>55-59</td><td>55-59</td><td>B</td><td>C</td><td>3.0</td><td>Good – generally sound work with a number of notable errors</td></tr>
<tr><td>50-54</td><td>50-54</td><td>B-</td><td>D</td><td>2.0</td><td>Satisfactory – fair but with significant shortcomings</td></tr>
<tr><td>45-49</td><td>45-49</td><td>C+</td><td>D</td><td>2.0</td><td>Satisfactory – fair but with significant shortcomings</td></tr>
<tr><td>41-44</td><td>41-44</td><td>C</td><td>E</td><td>1.0</td><td>Sufficient – performance meets the minimum criteria</td></tr>
<tr><td>40</td><td>40</td><td>C-</td><td>E</td><td>1.0</td><td>Sufficient – performance meets the minimum criteria</td></tr>
<tr><td>30-39</td><td>30-39</td><td>F</td><td>FX</td><td>0.0</td><td>Fail – some more work required such as retaking exam before the credit can be awarded</td></tr>
<tr><td>0-29</td><td>0-29</td><td>F</td><td>F</td><td>0.0</td><td>Fail – retake credits</td></tr>
</tbody>
</table>
<p><em>Grade Point = ECTS credit hours × ECTS Grade point</em></p>
<p><em>Grade Point Average = Total Grade Point / Total ECTS credit hours</em></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssFooter}"" alt=""Footer"" /></p>
";

    // Note: the original IBSS template carried a clause "3. Registration is not
    // considered complete without <<Missing documents>>…". That note is dropped
    // here because the partner-side flow now lets reviewers reject and request
    // re-uploads instead of issuing the offer with caveats. The remaining
    // numbered items renumber automatically inside <ol>.
    private static string DefaultOfferLetterHtml { get; } = $@"
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""MGW"" /></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSecondaryLogo}"" alt="""" /></p>
<h2 style=""text-align:center;"">Offer Letter</h2>
<p>Date: [date]</p>
<p>Ref: </p>
<p>Name: <strong>[student full name]</strong></p>
<p>Passport/ID No.: <strong>[passport id]</strong></p>
<p>Address: <strong>[student address]</strong></p>
<p></p>
<p>Dear <strong>[student full name]</strong>,</p>
<p>Congratulations. We are pleased to inform you that your application for <strong>My Global World Education Group (MGW)</strong> is approved. We look forward to having you with us. Our records for your admission will carry the following information:</p>
<table>
<tbody>
<tr><td><strong>Programme</strong></td><td>:</td><td>[program name]</td></tr>
<tr><td><strong>Specialization in</strong></td><td>:</td><td>[specialization name]</td></tr>
<tr><td><strong>Commencement date</strong></td><td>:</td><td>[commencement date]</td></tr>
<tr><td><strong>Expected completion date</strong></td><td>:</td><td>[completion date]</td></tr>
<tr><td><strong>Duration of study</strong></td><td>:</td><td>[duration of study]</td></tr>
<tr><td><strong>Learning center</strong></td><td>:</td><td>[partner name]</td></tr>
<tr><td><strong>Mode of study</strong></td><td>:</td><td>[mode of study]</td></tr>
<tr><td><strong>Instruction language</strong></td><td>:</td><td>[instruction language]</td></tr>
</tbody>
</table>
<ol>
<li>If you choose to accept/decline our offer, kindly respond by filling out the attached reply form within five (5) days of the date of this letter. We cannot guarantee a place in the programme and this offer may then be withdrawn if we do not receive any feedback within the stipulated time.</li>
<li>In the event that any information you had provided earlier is inaccurate or false, this offer of admission is considered null and void.</li>
<li>Upon acceptance to our offer, you are required to make the necessary payment to our partner [partner name].</li>
<li>Any refund after or before the class starts will be requested to our partner [partner name].</li>
<li>The duration of study is a maximum of [duration of study]. Should you exceed this study period, you will be charged a penalty fee.</li>
<li>The tuition fee is not covering the supervisor fee for the final project/dissertation project. Supervisor is not mandatory while doing final project/dissertation project. If the student wishes to have a supervisor from the school, please contact the school's registrar to have the updated supervisor fee.</li>
</ol>
<p><strong>My Global World Education Group (MGW)</strong> would like to congratulate you to join the programme in your quest towards academic and career advancement.</p>
<p>We wish you every success!</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssStamp}"" alt=""MGW Stamp"" /></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSignatureLine}"" alt=""Signature Line"" /></p>
<p>Anna Phan</p>
<p>Head of Administration</p>
<p></p>
<h3>(Please fill up this part)</h3>
<h3>Applicant's Confirmation</h3>
<p>By paying the tuition fee of the program, I, <strong>[student full name]</strong>, <strong>[passport id]</strong> accept the offer to study <strong>[program name]</strong> in My Global World Education Group (MGW). I hereby acknowledge that I have read and understand the terms and conditions of this offer letter and on the website (<a href=""https://ibss.edu.eu/"">https://ibss.edu.eu/</a>).</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssFooter}"" alt=""Footer"" /></p>
";

    private static string DefaultAdmissionLetterHtml { get; } = $@"
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""MGW"" /></p>
<p>Date: [date]</p>
<p>Ref: </p>
<h2 style=""text-align:center;"">Admission Letter</h2>
<p>Name: <strong>[student full name]</strong></p>
<p>Passport/ID No.: <strong>[passport id]</strong></p>
<p>Address: <strong>[student address]</strong></p>
<p></p>
<p>Dear <strong>[student full name]</strong>,</p>
<p><strong>My Global World Education Group (MGW)</strong> would like to take this opportunity to congratulate and welcome you to the programme in your quest towards academic and career advancement. It is our pleasure that you have been accepted into the programme.</p>
<table>
<tbody>
<tr><td><strong>Programme</strong></td><td>:</td><td>[program name]</td></tr>
<tr><td><strong>Specialization in</strong></td><td>:</td><td>[specialization name]</td></tr>
<tr><td><strong>Student ID</strong></td><td>:</td><td>[student number]</td></tr>
<tr><td><strong>Commencement date</strong></td><td>:</td><td>[commencement date]</td></tr>
<tr><td><strong>Duration of study</strong></td><td>:</td><td>[duration of study]</td></tr>
<tr><td><strong>Learning center</strong></td><td>:</td><td>[partner name]</td></tr>
<tr><td><strong>Mode of study</strong></td><td>:</td><td>[mode of study]</td></tr>
</tbody>
</table>
<p>We hereby confirm to register you as our active student for our program as mentioned above.</p>
<p>Participation in this programme is governed by MGW Terms &amp; Conditions (see <a href=""http://ibss.edu.eu/"">http://ibss.edu.eu/</a>).</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssStamp}"" alt=""MGW Stamp"" /></p>
<p>Thank you,</p>
<p>Yours sincerely,</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSignatureLine}"" alt=""Signature Line"" /></p>
<p>Anna Phan</p>
<p>Head of Administration</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssFooter}"" alt=""Footer"" /></p>
";

    /// <summary>
    /// Seeds (and keeps in sync) the system-generated <see cref="DocumentType"/>
    /// rows that back released letter PDFs. Stable Guids from
    /// <see cref="SystemDocumentTypeIds"/> are used so the letter pipeline can
    /// reference them directly. Idempotent.
    /// </summary>
    private static async Task SeedSystemDocumentTypesAsync(OdinDbContext context, ILogger logger)
    {
        var existingIds = await context.DocumentTypes
            .Select(d => d.DocumentTypeId)
            .ToListAsync();
        var existingSet = existingIds.ToHashSet();

        var added = 0;
        foreach (var seed in SystemDocumentTypeIds.All)
        {
            if (existingSet.Contains(seed.Id)) continue;
            context.DocumentTypes.Add(new DocumentType
            {
                DocumentTypeId = seed.Id,
                Name = seed.Name,
                Description = seed.Description,
                // Student Card Picture is the one entry in this list that is
                // UPLOADED (by the student or the Admission Office), not
                // system-generated — it must stay pickable in upload dialogs.
                IsSystemGenerated = seed.Id != SystemDocumentTypeIds.StudentCardPicture,
            });
            added++;
        }

        // Data fix for rows seeded before the rule above existed.
        var cardPic = await context.DocumentTypes
            .FirstOrDefaultAsync(d => d.DocumentTypeId == SystemDocumentTypeIds.StudentCardPicture && d.IsSystemGenerated);
        if (cardPic is not null)
        {
            cardPic.IsSystemGenerated = false;
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Student Card Picture: marked uploadable (was system-generated)");
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] System document types: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] System document types already complete — skipping");
        }
    }

    /// <summary>
    /// Seeds the standard set of supporting documents an applicant may be asked
    /// to upload. Idempotent on Name (case-insensitive) so re-runs only add
    /// missing rows; admin edits via the System Config tab are preserved.
    /// </summary>
    private static async Task SeedDocumentTypesAsync(OdinDbContext context, ILogger logger)
    {
        var seed = new (string Name, string Description)[]
        {
            ("Passport",                          "Passport bio-data page (machine-readable, valid for 6+ months)."),
            ("National ID",                       "National identity card (front and back) where a passport is unavailable."),
            ("Birth Certificate",                 "Official civil birth registration document."),
            ("Passport Photograph",               "Recent passport-style photo against a plain background."),
            ("Curriculum Vitae",                  "Up-to-date CV / résumé covering education and employment history."),
            ("High School Certificate",           "Secondary school leaving certificate (e.g. high school diploma, A-levels, abitur, baccalauréat)."),
            ("High School Transcript",            "Final-year transcript / grade report from secondary school."),
            ("Diploma Certificate",               "Awarding certificate for a Diploma qualification."),
            ("Diploma Transcript",                "Academic transcript for the Diploma award."),
            ("Advanced Diploma Certificate",      "Awarding certificate for an Advanced Diploma."),
            ("Advanced Diploma Transcript",       "Academic transcript for the Advanced Diploma award."),
            ("Associate Degree Certificate",      "Awarding certificate for an Associate Degree."),
            ("Associate Degree Transcript",       "Academic transcript for the Associate Degree."),
            ("Bachelor's Degree Certificate",     "Awarding certificate for an undergraduate Bachelor's degree."),
            ("Bachelor's Degree Transcript",      "Academic transcript covering all years of the Bachelor's programme."),
            ("Master's Degree Certificate",       "Awarding certificate for a Master's degree."),
            ("Master's Degree Transcript",        "Academic transcript for the Master's programme."),
            ("Doctorate / PhD Certificate",       "Awarding certificate for a Doctoral / PhD degree."),
            ("Doctorate / PhD Transcript",        "Academic transcript / dissertation summary for the Doctoral programme."),
            ("Language Proficiency Certificate",  "Recognised English language test result (IELTS, TOEFL, Duolingo, Cambridge, etc.)."),
            ("Letter of Motivation",              "Personal statement explaining why the applicant is pursuing the programme."),
            ("Letter of Recommendation",          "Reference letter from an academic or professional referee."),
            ("Work Experience Certificate",       "Employer-issued letter confirming role, dates and responsibilities."),
            ("Professional Certifications",       "Industry or vocational certifications relevant to the programme."),
            ("Research Proposal",                 "Proposed research topic and methodology (PhD / DBA applicants)."),
            ("Portfolio",                         "Body-of-work portfolio (creative or technical programmes)."),
            ("Proof of Funds",                    "Bank statement or sponsor letter evidencing tuition / living-cost cover."),
            ("Sponsorship Letter",                "Letter from sponsor (employer, government, family) committing to fund studies."),
            ("Visa / Residence Permit",           "Current visa or residence-permit document (if applicable)."),
            ("Other Supporting Document",         "Any additional document the applicant or partner deems relevant."),
        };

        // Compare on the normalised (trim + lower) name so admins editing casing
        // doesn't trigger duplicates on the next boot.
        var existingNames = await context.DocumentTypes
            .Select(d => d.Name)
            .ToListAsync();
        var existingSet = existingNames
            .Select(n => n.Trim().ToLowerInvariant())
            .ToHashSet();

        var added = 0;
        foreach (var (name, description) in seed)
        {
            if (existingSet.Contains(name.Trim().ToLowerInvariant())) continue;
            context.DocumentTypes.Add(new DocumentType
            {
                DocumentTypeId = Guid.NewGuid(),
                Name = name,
                Description = description,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Document types: +{Count} added (had {Existing})", added, existingNames.Count);
        }
        else
        {
            logger.LogInformation("[Seeder] Document types already complete ({Existing} rows) — skipping", existingNames.Count);
        }
    }

    /// <summary>
    /// Seeds the prior-education levels referenced by Pathways.
    /// Returns a name → Guid map so callers can resolve references without
    /// re-querying. Idempotent on Name.
    /// </summary>
    private static async Task SeedPositionFunctionsAsync(OdinDbContext context, ILogger logger)
    {
        var seed = new (string Name, int DisplayOrder)[]
        {
            ("Consulting",             100),
            ("Finance - Accounting",   200),
            ("General Management",     300),
            ("Human Resources",        400),
            ("Marketing - Sales",      500),
            ("Information Technology",  600),
            ("Operation - Logistics",  700),
            ("Others",                 900),
        };

        var existing = await context.PositionFunctions.Select(e => e.Name.ToLower()).ToListAsync();
        var have = existing.ToHashSet();
        var added = 0;
        foreach (var (name, displayOrder) in seed)
        {
            if (have.Contains(name.Trim().ToLowerInvariant())) continue;
            context.PositionFunctions.Add(new PositionFunction { PositionFunctionId = Guid.NewGuid(), Name = name, DisplayOrder = displayOrder });
            added++;
        }
        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Position functions: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Position functions already complete — skipping");
        }
    }

    private static async Task SeedEmploymentIndustriesAsync(OdinDbContext context, ILogger logger)
    {
        var seed = new (string Name, int DisplayOrder)[]
        {
            ("Consulting",                                100),
            ("Consumer Packaged Goods",                   200),
            ("Energy",                                    300),
            ("Financial Services",                        400),
            ("Government",                                500),
            ("Healthcare (including products and services)", 600),
            ("Hospitality",                               700),
            ("Manufacturing",                             800),
            ("Media and Entertainment",                   900),
            ("Non-profit",                               1000),
            ("Real estate",                              1100),
            ("Retail",                                   1200),
            ("Technology",                               1300),
            ("Transportation and Logistics Services",    1400),
            ("Others",                                   1900),
        };

        // Data fix: an earlier seed mistakenly split the single spec entry
        // "Transportation and Logistics Services" into "Transportation and
        // Logistics" + "Services". Rename the former and retire the latter
        // (only when unused). Idempotent — later boots find nothing to do.
        var wrongTransport = await context.EmploymentIndustries
            .FirstOrDefaultAsync(e => e.Name == "Transportation and Logistics" && e.DeletedAt == null);
        if (wrongTransport is not null
            && !await context.EmploymentIndustries.AnyAsync(e => e.Name == "Transportation and Logistics Services" && e.DeletedAt == null))
        {
            wrongTransport.Name = "Transportation and Logistics Services";
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Employment industries: renamed 'Transportation and Logistics' to include 'Services'");
        }
        var spuriousServices = await context.EmploymentIndustries
            .FirstOrDefaultAsync(e => e.Name == "Services" && e.DeletedAt == null);
        if (spuriousServices is not null
            && !await context.Students.AnyAsync(s => s.CurrentEmploymentIndustryId == spuriousServices.EmploymentIndustryId))
        {
            spuriousServices.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Employment industries: retired spurious 'Services' entry");
        }

        var existing = await context.EmploymentIndustries.Select(e => e.Name.ToLower()).ToListAsync();
        var have = existing.ToHashSet();
        var added = 0;
        foreach (var (name, displayOrder) in seed)
        {
            if (have.Contains(name.Trim().ToLowerInvariant())) continue;
            context.EmploymentIndustries.Add(new EmploymentIndustry { EmploymentIndustryId = Guid.NewGuid(), Name = name, DisplayOrder = displayOrder });
            added++;
        }
        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Employment industries: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Employment industries already complete — skipping");
        }
    }

    private static async Task<Dictionary<string, Guid>> SeedEducationLevelsAsync(
        OdinDbContext context, ILogger logger)
    {
        // Rank uses powers-of-100 so admins can splice in intermediate levels
        // (e.g. "Postgraduate Certificate") without rebalancing existing rows.
        var seed = new (string Name, int Rank, int DisplayOrder)[]
        {
            ("High School Certificate", 100, 100),
            ("Diploma",                 200, 200),
            ("Associate Degree",        200, 210),
            ("Advanced Diploma",        300, 300),
            ("Bachelor's Degree",       400, 400),
            ("Postgraduate Diploma",    450, 450),
            ("Master's Degree",         500, 500),
            ("Doctorate / PhD",         600, 600),
        };

        var existing = await context.EducationLevels.ToListAsync();
        var byName = existing.ToDictionary(e => e.Name.Trim().ToLowerInvariant(), e => e.EducationLevelId);

        var added = 0;
        foreach (var (name, rank, displayOrder) in seed)
        {
            var key = name.Trim().ToLowerInvariant();
            if (byName.ContainsKey(key)) continue;

            var entity = new EducationLevel
            {
                EducationLevelId = Guid.NewGuid(),
                Name = name,
                Rank = rank,
                DisplayOrder = displayOrder,
            };
            context.EducationLevels.Add(entity);
            byName[key] = entity.EducationLevelId;
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Education levels: +{Count} added (had {Existing})", added, existing.Count);
        }
        else
        {
            logger.LogInformation("[Seeder] Education levels already complete ({Existing} rows) — skipping", existing.Count);
        }

        // Return a friendly map keyed on the seed name (case-insensitive).
        return seed.ToDictionary(s => s.Name, s => byName[s.Name.Trim().ToLowerInvariant()]);
    }

    private sealed record SeedPathway(
        string Name,
        string Description,
        int MinimumYearsWorkExperience,
        string[] AcceptedEducationLevels,
        string[] DocumentRequirements);

    /// <summary>
    /// Seeds IBSS entry pathways with required documents and accepted prior
    /// education levels. Idempotent on pathway Name.
    /// </summary>
    private static async Task SeedPathwaysAsync(
        OdinDbContext context, ILogger logger, Dictionary<string, Guid> eduLevelByName)
    {
        // Document types are referenced by name. Build a map once.
        var docByName = (await context.DocumentTypes.Where(d => d.DeletedAt == null).ToListAsync())
            .ToDictionary(d => d.Name.Trim().ToLowerInvariant(), d => d.DocumentTypeId);

        // ── Convenience handles ─────────────────────────────────────────────
        const string HighSchool        = "High School Certificate";
        const string Diploma           = "Diploma";
        const string Associate         = "Associate Degree";
        const string AdvDiploma        = "Advanced Diploma";
        const string Bachelor          = "Bachelor's Degree";
        const string PgDip             = "Postgraduate Diploma";
        const string Master            = "Master's Degree";
        const string Doctorate         = "Doctorate / PhD";

        // Documents universal to every pathway.
        var commonDocs = new[]
        {
            "Passport",
            "Curriculum Vitae",
            "Language Proficiency Certificate",
        };

        // Helper: pathway docs = common + extras (de-duplicated, order preserved).
        string[] Docs(params string[] extras) => commonDocs.Concat(extras).Distinct().ToArray();

        var seed = new[]
        {
            // ── Master ───────────────────────────────────────────────────────
            new SeedPathway(
                "Master — Pathway One: Direct Entry via Bachelor's Degree",
                "Hold a Bachelor's degree in any discipline (preferably in Business). The degree must be equivalent to a minimum of 180 ECTS credits or a recognised equivalent (e.g., UK Level 6, US 4-year degree). No work experience required.",
                MinimumYearsWorkExperience: 0,
                new[] { Bachelor, PgDip, Master, Doctorate },
                Docs("Bachelor's Degree Certificate", "Bachelor's Degree Transcript")),
            new SeedPathway(
                "Master — Pathway Two: Advanced Diploma + 3 Years Work Experience",
                "Hold an Advanced Diploma in any discipline (preferably in Business). The qualification must be equivalent to a minimum of 120 ECTS credits. Provide evidence of at least 3 years of work experience. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 3,
                new[] { AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Advanced Diploma Certificate", "Advanced Diploma Transcript", "Work Experience Certificate")),
            new SeedPathway(
                "Master — Pathway Three: Diploma + 5 Years Work Experience",
                "Hold a Diploma or Associate Degree in any discipline (preferably in Business). The qualification must be equivalent to a minimum of 60 ECTS credits. Provide evidence of at least 5 years of work experience. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 5,
                new[] { Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Diploma Certificate", "Diploma Transcript", "Work Experience Certificate")),
            new SeedPathway(
                "Master — Pathway Four: High School + 8 Years Work Experience",
                "Hold a High School Certificate (e.g., SPM, GCSEs, IB, or equivalent). Provide evidence of at least 8 years of work experience (preference will be given to applicants with business management experience). Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 8,
                new[] { HighSchool, Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("High School Certificate", "High School Transcript", "Work Experience Certificate")),

            // ── Doctor ───────────────────────────────────────────────────────
            new SeedPathway(
                "Doctor — Pathway One: Master's Degree (Preferred Entry)",
                "A Master's degree in any discipline (Business preferred), or an equivalent postgraduate qualification. No work experience required.",
                MinimumYearsWorkExperience: 0,
                new[] { PgDip, Master, Doctorate },
                Docs("Master's Degree Certificate", "Master's Degree Transcript", "Research Proposal")),
            new SeedPathway(
                "Doctor — Pathway Two: Bachelor's Degree + 5 Years Work Experience",
                "A Bachelor's degree in any discipline (Business preferred), equivalent to a minimum of 180 ECTS or its international equivalent, plus a minimum of 5 years of evidenced work experience. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 5,
                new[] { Bachelor, PgDip, Master, Doctorate },
                Docs("Bachelor's Degree Certificate", "Bachelor's Degree Transcript", "Work Experience Certificate", "Research Proposal")),
            new SeedPathway(
                "Doctor — Pathway Three: Advanced Diploma + 7 Years Work Experience",
                "An Advanced Diploma in any discipline (Business preferred), equivalent to a minimum of 120 ECTS, plus a minimum of 7 years of evidenced work experience. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 7,
                new[] { AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Advanced Diploma Certificate", "Advanced Diploma Transcript", "Work Experience Certificate", "Research Proposal")),
            new SeedPathway(
                "Doctor — Pathway Four: Diploma + 9 Years Work Experience",
                "A Diploma or Associate Diploma in any discipline (Business preferred), equivalent to a minimum of 60 ECTS, plus a minimum of 9 years of evidenced work experience. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 9,
                new[] { Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Diploma Certificate", "Diploma Transcript", "Work Experience Certificate", "Research Proposal")),
            new SeedPathway(
                "Doctor — Pathway Five: High School Certificate + 12 Years Work Experience",
                "A recognized High School Certificate, plus a minimum of 12 years of evidenced work experience (preference is given to applicants with business management experience). Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV.",
                MinimumYearsWorkExperience: 12,
                new[] { HighSchool, Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("High School Certificate", "High School Transcript", "Work Experience Certificate", "Research Proposal")),

            // ── Diploma ──────────────────────────────────────────────────────
            new SeedPathway(
                "Diploma — Open Entry",
                "Open Entry. Diploma-level admission with no prior-qualification restriction.",
                MinimumYearsWorkExperience: 0,
                Array.Empty<string>(), // empty → no restriction (wizard convention)
                Docs()),

            // ── Advanced Diploma ─────────────────────────────────────────────
            new SeedPathway(
                "Advanced Diploma — Pathway One: Diploma or Associate Degree",
                "Hold a Diploma or Associate Diploma in any discipline (Business preferred). The qualification must be equivalent to a minimum of 60 ECTS or its international equivalent. No work experience required.",
                MinimumYearsWorkExperience: 0,
                new[] { Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Diploma Certificate", "Diploma Transcript")),
            new SeedPathway(
                "Advanced Diploma — Pathway Two: High School Certificate + 3 Years Work Experience",
                "Possess a recognized High School Certificate or its equivalent (e.g. STPM, IGCSE, A-Levels, UEC). Plus a minimum of 3 years of full-time work experience in any field. Work experience must be formally documented and verifiable through employer letters, employment records, or the applicant's CV. Applicants with non-business backgrounds are accepted but may be advised to take introductory business modules as part of the program.",
                MinimumYearsWorkExperience: 3,
                new[] { HighSchool, Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("High School Certificate", "High School Transcript", "Work Experience Certificate")),

            // ── Bachelor Top-Up ──────────────────────────────────────────────
            new SeedPathway(
                "Bachelor Top-Up — Pathway One: Advanced Diploma",
                "Hold an Advanced Diploma in any discipline (Business preferred). The qualification must be equivalent to a minimum of 120 ECTS or its internationally recognized equivalent. No work experience required.",
                MinimumYearsWorkExperience: 0,
                new[] { AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Advanced Diploma Certificate", "Advanced Diploma Transcript")),
            new SeedPathway(
                "Bachelor Top-Up — Pathway Two: Diploma + 2 Years Work Experience",
                "Hold a Diploma or Associate Diploma in any discipline (Business preferred). The qualification must be equivalent to a minimum of 60 ECTS, plus a minimum of 2 years of full-time work experience in any field.",
                MinimumYearsWorkExperience: 2,
                new[] { Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("Diploma Certificate", "Diploma Transcript", "Work Experience Certificate")),
            new SeedPathway(
                "Bachelor Top-Up — Pathway Three: High School Certificate + 5 Years Work Experience",
                "Hold a recognized High School Certificate (e.g., STPM, IGCSE, A-Levels, National Secondary Certificate). Plus a minimum of 5 years of full-time work experience in any field.",
                MinimumYearsWorkExperience: 5,
                new[] { HighSchool, Diploma, Associate, AdvDiploma, Bachelor, PgDip, Master, Doctorate },
                Docs("High School Certificate", "High School Transcript", "Work Experience Certificate")),
        };

        // ── Insert pathways missing from the DB ─────────────────────────────
        var existingPathways = await context.Pathways.ToListAsync();
        // GroupBy→First: concurrent seeders (dev+prod share the DB) once
        // raced and double-inserted a pathway — never crash on duplicates.
        var pathwayByName = existingPathways
            .GroupBy(p => p.Name.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var pathwaysAdded = 0;
        foreach (var s in seed)
        {
            var key = s.Name.Trim().ToLowerInvariant();
            if (pathwayByName.ContainsKey(key)) continue;

            var entity = new Pathway
            {
                PathwayId = Guid.NewGuid(),
                Name = s.Name,
                Description = s.Description,
                MinimumYearsWorkExperience = s.MinimumYearsWorkExperience,
            };
            context.Pathways.Add(entity);
            pathwayByName[key] = entity;
            pathwaysAdded++;
        }

        if (pathwaysAdded > 0)
            await context.SaveChangesAsync();

        // ── Wire accepted education levels (skip rows already present) ─────
        var existingAccepted = await context.PathwayAcceptedEducationLevels
            .Select(x => new { x.PathwayId, x.EducationLevelId })
            .ToListAsync();
        var acceptedSet = existingAccepted
            .Select(x => (x.PathwayId, x.EducationLevelId))
            .ToHashSet();

        var acceptedAdded = 0;
        foreach (var s in seed)
        {
            var pathway = pathwayByName[s.Name.Trim().ToLowerInvariant()];
            foreach (var eduName in s.AcceptedEducationLevels)
            {
                if (!eduLevelByName.TryGetValue(eduName, out var eduId))
                {
                    logger.LogWarning("[Seeder] Pathway '{Pathway}' references unknown education level '{Edu}' — skipping",
                        s.Name, eduName);
                    continue;
                }
                if (acceptedSet.Contains((pathway.PathwayId, eduId))) continue;
                context.PathwayAcceptedEducationLevels.Add(new PathwayAcceptedEducationLevel
                {
                    PathwayId = pathway.PathwayId,
                    EducationLevelId = eduId,
                });
                acceptedSet.Add((pathway.PathwayId, eduId));
                acceptedAdded++;
            }
        }

        // ── Wire document requirements (skip rows already present) ─────────
        var existingReqs = await context.PathwayDocumentRequirements
            .Where(r => r.DeletedAt == null)
            .Select(r => new { r.PathwayId, r.DocumentTypeId })
            .ToListAsync();
        var reqSet = existingReqs
            .Select(r => (r.PathwayId, r.DocumentTypeId))
            .ToHashSet();

        var reqsAdded = 0;
        foreach (var s in seed)
        {
            var pathway = pathwayByName[s.Name.Trim().ToLowerInvariant()];
            foreach (var docName in s.DocumentRequirements)
            {
                if (!docByName.TryGetValue(docName.Trim().ToLowerInvariant(), out var docId))
                {
                    logger.LogWarning("[Seeder] Pathway '{Pathway}' references unknown document type '{Doc}' — skipping",
                        s.Name, docName);
                    continue;
                }
                if (reqSet.Contains((pathway.PathwayId, docId))) continue;
                context.PathwayDocumentRequirements.Add(new PathwayDocumentRequirement
                {
                    PathwayDocumentRequirementId = Guid.NewGuid(),
                    PathwayId = pathway.PathwayId,
                    DocumentTypeId = docId,
                });
                reqSet.Add((pathway.PathwayId, docId));
                reqsAdded++;
            }
        }

        if (acceptedAdded > 0 || reqsAdded > 0)
            await context.SaveChangesAsync();

        logger.LogInformation(
            "[Seeder] Pathways: +{Pathways} added (had {Existing}); +{Accepted} accepted-education-level rows; +{Reqs} document-requirement rows",
            pathwaysAdded, existingPathways.Count, acceptedAdded, reqsAdded);
    }

    /// <summary>
    /// Seeds the four IBSS core programmes (BBA, MBA, DBA, EDBA) with their
    /// specializations and per-specialization subjects. Idempotent on
    /// Programme.Code — if a code is already present, that programme is left
    /// alone (admin edits are preserved).
    /// </summary>
    private static async Task SeedIbssCoreProgrammesAsync(OdinDbContext context, ILogger logger)
    {
        var existingCodes = (await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.Code)
            .ToListAsync())
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        // Resolve award education levels by name (seeded earlier in
        // SeedEducationLevelsAsync). The mapping below is used both for new
        // programme creation and for the idempotent backfill that follows.
        var eduByName = await context.EducationLevels
            .Where(e => e.DeletedAt == null)
            .ToDictionaryAsync(e => e.Name.Trim().ToLowerInvariant(), e => e.EducationLevelId);
        Guid? AwardId(string name) => eduByName.TryGetValue(name.Trim().ToLowerInvariant(), out var id) ? id : null;
        var bbaAward  = AwardId("Bachelor's Degree");
        var mbaAward  = AwardId("Master's Degree");
        var docAward  = AwardId("Doctorate / PhD");

        var added = 0;

        // ── BBA: 21 shared core modules across 5 specializations ────────────
        var bbaCore = new (string Code, string Name, int Ects)[]
        {
            ("BBA-101", "Business Environment", 5),
            ("BBA-102", "Resource Management", 5),
            ("BBA-103", "Communication Skills", 5),
            ("BBA-104", "People in Organisations", 5),
            ("BBA-105", "Administrative Services", 5),
            ("BBA-106", "Personal & Professional Development", 5),
            ("BBA-107", "Internship & Internship Report (Year 1)", 5),
            ("BBA-201", "Managing Communication", 5),
            ("BBA-202", "Business Organisations in a Global Context", 5),
            ("BBA-203", "People Management", 5),
            ("BBA-204", "Finance for Managers", 5),
            ("BBA-205", "Employability Skills", 5),
            ("BBA-206", "Business Ethics", 5),
            ("BBA-207", "Internship & Internship Report (Year 2)", 5),
            ("BBA-301", "Planning a New Business Venture", 5),
            ("BBA-302", "Risk Management", 5),
            ("BBA-303", "Customer Relationship Management", 5),
            ("BBA-304", "Leadership & Management", 5),
            ("BBA-305", "Managing Quality and Service Delivery", 5),
            ("BBA-306", "Personal Leadership & Management Development", 5),
            ("BBA-307", "Internship & Internship Report (Year 3)", 5),
        };
        var bbaSpecs = new (string SpecCode, string SpecName, (string Code, string Name, int Ects)[] Year4)[]
        {
            ("BBA-GEN", "General",                  new[] { ("BBA-G401","Marketing Communications",6), ("BBA-G402","Sales",6), ("BBA-G403","Project Management",6), ("BBA-G404","Human Resource Management",6), ("BBA-G499","Capstone Project",12) }),
            ("BBA-FIN", "Finance",                  new[] { ("BBA-F401","Financial Decision Making for Managers",6), ("BBA-F402","Accounting",6), ("BBA-F403","Managing Finance in the Public Sector",6), ("BBA-F404","Economics for Business",6), ("BBA-F499","Capstone Project",12) }),
            ("BBA-PEO", "Managing People",          new[] { ("BBA-P401","Human Resource Management",6), ("BBA-P402","Managing Change",6), ("BBA-P403","Sales",6), ("BBA-P404","Project Management",6), ("BBA-P499","Capstone Project",12) }),
            ("BBA-OPS", "Operations Management",    new[] { ("BBA-O401","Logistics & Supply Chain Management",6), ("BBA-O402","Managing Change",6), ("BBA-O403","Risk Management",6), ("BBA-O404","Project Management",6), ("BBA-O499","Capstone Project",12) }),
            ("BBA-SAM", "Sales and Marketing",      new[] { ("BBA-S401","Factors Determining Marketing Strategies",6), ("BBA-S402","Marketing Communications",6), ("BBA-S403","Sales",6), ("BBA-S404","Branding",6), ("BBA-S499","Capstone Project",12) }),
        };
        if (!existingCodes.Contains("BBA"))
        {
            AddProgramme("BBA",
                "Bachelor of Business Administration",
                "Three-year undergraduate Bachelor's covering core business fundamentals followed by a fourth specialisation year and capstone project.",
                bbaAward,
                bbaSpecs.Select(s => (s.SpecCode, s.SpecName, "Year-4 specialisation in " + s.SpecName + ".", 48,
                    bbaCore.Concat(s.Year4).ToArray())).ToArray());
            added++;
        }

        // ── MBA: 5 shared core modules + 3 spec modules + Final Thesis ─────
        var mbaCore = new (string Code, string Name, int Ects)[]
        {
            ("MBA-C01", "Strategic Planning", 8),
            ("MBA-C02", "Finance for Strategic Managers", 8),
            ("MBA-C03", "Research for Strategic Development", 8),
            ("MBA-C04", "Organisational Behaviour", 8),
            ("MBA-C05", "Personal Development for Leadership and Strategic Management", 8),
        };
        var mbaThesis = new (string Code, string Name, int Ects)[] { ("MBA-T99", "Final Thesis", 24) };
        var mbaSpecs = new (string SpecCode, string SpecName, (string Code, string Name, int Ects)[] Spec)[]
        {
            ("MBA-GEN", "General",                    new[] { ("MBA-G01","International Business Environment",8), ("MBA-G02","International Marketing",8), ("MBA-G03","Strategic Human Resource Management",8) }),
            ("MBA-IF",  "International Finance",      new[] { ("MBA-IF1","International Business Environment",8), ("MBA-IF2","Developing Organisational Vision & Strategic Direction",8), ("MBA-IF3","International Finance",8) }),
            ("MBA-HR",  "Human Resource Management",  new[] { ("MBA-HR1","Developing Organisational Vision & Strategic Direction",8), ("MBA-HR2","Managing Continuous Organisational Improvement",8), ("MBA-HR3","Strategic Human Resource Management",8) }),
            ("MBA-IB",  "International Business",     new[] { ("MBA-IB1","International Business Environment",8), ("MBA-IB2","International Marketing",8), ("MBA-IB3","International Finance",8) }),
            ("MBA-MK",  "Marketing",                  new[] { ("MBA-MK1","Corporate Communication Strategies",8), ("MBA-MK2","International Marketing",8), ("MBA-MK3","Strategic Marketing",8) }),
            ("MBA-MG",  "Management",                 new[] { ("MBA-MG1","Corporate Communication Strategies",8), ("MBA-MG2","International Marketing",8), ("MBA-MG3","Strategic Human Resource Management",8) }),
        };
        if (!existingCodes.Contains("MBA"))
        {
            AddProgramme("MBA",
                "Master of Business Administration",
                "Postgraduate Master's combining a shared business-strategy core with a chosen specialisation and a final thesis.",
                mbaAward,
                mbaSpecs.Select(s => (s.SpecCode, s.SpecName, "MBA specialisation in " + s.SpecName + ".", 18,
                    mbaCore.Concat(s.Spec).Concat(mbaThesis).ToArray())).ToArray());
            added++;
        }

        // ── DBA: single General specialization ──────────────────────────────
        var dbaModules = new (string Code, string Name, int Ects)[]
        {
            ("DBA-01", "Research Philosophy", 12),
            ("DBA-02", "Research Methodology", 12),
            ("DBA-03", "Advanced Statistics", 12),
            ("DBA-04", "Multivariate Analysis", 12),
            ("DBA-05", "Research Proposal", 12),
            ("DBA-99", "Final Research Paper", 90),
        };
        if (!existingCodes.Contains("DBA"))
        {
            AddProgramme("DBA",
                "Doctor of Business Administration",
                "Doctoral programme building advanced research skills and culminating in an independent research paper.",
                docAward,
                new[] { ("DBA-GEN", "General", "DBA general track.", 36, dbaModules) });
            added++;
        }

        // ── Executive DBA: single General specialization ────────────────────
        var edbaModules = new (string Code, string Name, int Ects)[]
        {
            ("EDBA-01", "Research Philosophy", 9),
            ("EDBA-02", "International Business", 9),
            ("EDBA-03", "Advanced Statistics", 9),
            ("EDBA-04", "Managerial Economics", 9),
            ("EDBA-05", "Organisational Leadership", 9),
            ("EDBA-99", "Dissertation (Research Paper)", 60),
        };
        if (!existingCodes.Contains("EDBA"))
        {
            AddProgramme("EDBA",
                "Executive Doctor of Business Administration",
                "Executive-format doctorate for senior practitioners, with shorter taught modules and a substantial dissertation.",
                docAward,
                new[] { ("EDBA-GEN", "General", "Executive DBA general track.", 24, edbaModules) });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] IBSS core programmes: +{Count} added", added);
        }
        else
        {
            logger.LogInformation("[Seeder] IBSS core programmes already present — skipping");
        }

        // Backfill: any pre-existing IBSS core programme with a NULL award gets
        // its level filled in. Safe to re-run; only updates rows that need it.
        var backfillMap = new (string Code, Guid? AwardId)[]
        {
            ("BBA", bbaAward),
            ("MBA", mbaAward),
            ("DBA", docAward),
            ("EDBA", docAward),
        };
        var backfilled = 0;
        foreach (var (code, awardId) in backfillMap)
        {
            if (awardId is null) continue;
            var existing = await context.Programmes
                .FirstOrDefaultAsync(p => p.Code == code && p.DeletedAt == null && p.AwardEducationLevelId == null);
            if (existing is null) continue;
            existing.AwardEducationLevelId = awardId;
            backfilled++;
        }
        if (backfilled > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] IBSS core programmes: backfilled award level on {Count} row(s)", backfilled);
        }

        // Helper closure: builds Programme + Specializations + Subjects in-memory.
        void AddProgramme(
            string code,
            string name,
            string description,
            Guid? awardEducationLevelId,
            IReadOnlyList<(string SpecCode, string SpecName, string SpecDescription, int DurationMonths,
                           (string Code, string Name, int Ects)[] Subjects)> specializations)
        {
            var programme = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Programme
            {
                ProgrammeId = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = description,
                OwnerId = null, // null = IBSS core, not partner-owned
                AwardEducationLevelId = awardEducationLevelId,
            };
            context.Programmes.Add(programme);

            foreach (var s in specializations)
            {
                var spec = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Specialization
                {
                    SpecializationId = Guid.NewGuid(),
                    ProgrammeId = programme.ProgrammeId,
                    Code = s.SpecCode,
                    Name = s.SpecName,
                    Description = s.SpecDescription,
                    DurationOfStudyMonths = s.DurationMonths,
                    InstructionLanguage = "English", // IBSS is English-medium
                    IsActive = DateTime.UtcNow,
                };
                context.Specializations.Add(spec);

                foreach (var (subjCode, subjName, ects) in s.Subjects)
                {
                    context.Subjects.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.Subject
                    {
                        SubjectId = Guid.NewGuid(),
                        SpecializationId = spec.SpecializationId,
                        Code = subjCode,
                        Name = subjName,
                        Description = subjName,
                        Ects = ects,
                        IsActive = DateTime.UtcNow,
                    });
                }
            }
        }
    }

    private static string GeneratePassword()
    {
        // 16-char password: letters + digits + one special char, guaranteed complexity
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$%&*";
        const string all     = upper + lower + digits + special;

        var rng = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(rng);

        var chars = new char[16];
        // Guarantee one of each required category at fixed positions
        chars[0]  = upper[rng[0]  % upper.Length];
        chars[1]  = lower[rng[1]  % lower.Length];
        chars[2]  = digits[rng[2] % digits.Length];
        chars[3]  = special[rng[3] % special.Length];
        for (int i = 4; i < 16; i++)
            chars[i] = all[rng[i] % all.Length];

        // Shuffle
        System.Security.Cryptography.RandomNumberGenerator.Fill(rng);
        for (int i = 15; i > 0; i--)
        {
            int j = rng[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static async Task<(byte[] OprfSeed, byte[] ClientPublicKey, byte[] KemPublicKey, byte[] KemEncryptedPrivKey, byte[] KemNonce)>
        ComputeOpaqueCredentials(string password, ILogger logger)
    {
        var webProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Odin.Web"));
        var scriptPath    = Path.Combine(webProjectDir, "scripts", "compute-opaque-credentials.mjs");

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"OPAQUE credential script not found at: {scriptPath}");

        var psi = new ProcessStartInfo("node", $"\"{scriptPath}\" \"{password}\"")
        {
            WorkingDirectory    = webProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute     = false,
            CreateNoWindow      = true
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error  = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger.LogError("[Seeder] compute-opaque-credentials.mjs failed: {Error}", error);
            throw new InvalidOperationException($"OPAQUE credential generation failed: {error}");
        }

        var json              = JsonDocument.Parse(output);
        var oprfSeed          = Convert.FromBase64String(json.RootElement.GetProperty("oprfSeed").GetString()!);
        var clientPublicKey   = Convert.FromBase64String(json.RootElement.GetProperty("clientPublicKey").GetString()!);
        var kemPublicKey      = Convert.FromBase64String(json.RootElement.GetProperty("kemPublicKey").GetString()!);
        var kemEncPrivKey     = Convert.FromBase64String(json.RootElement.GetProperty("kemEncryptedPrivKey").GetString()!);
        var kemNonce          = Convert.FromBase64String(json.RootElement.GetProperty("kemNonce").GetString()!);

        return (oprfSeed, clientPublicKey, kemPublicKey, kemEncPrivKey, kemNonce);
    }

    /// <summary>
    /// Seeds the ready-made student evaluation questionnaires (school,
    /// education, teacher, Moodle, overall satisfaction) plus the Career &amp;
    /// Recruitment Data questionnaire. Insert-by-name only: an admin's edits
    /// or deletions in the builder are never overwritten or resurrected —
    /// a soft-deleted template with the same name blocks re-seeding on purpose.
    /// </summary>
    private static async Task SeedDefaultQuestionnairesAsync(OdinDbContext context, ILogger logger)
    {
        var existingNames = (await context.QuestionnaireTemplates
                .IgnoreQueryFilters()
                .Select(t => t.Name)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var (name, json) in DefaultQuestionnaires.All())
        {
            if (existingNames.Contains(name)) continue;
            context.QuestionnaireTemplates.Add(new SharedLibrary.Basics.Opaque.Domains.Intake.QuestionnaireTemplate
            {
                Name = name,
                Version = "1.0.0",
                DefinitionJson = json,
                DefinitionHash = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))),
            });
            added++;
        }
        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default questionnaires: +{Count} seeded", added);
        }
    }

    /// <summary>
    /// Ready-made partnership document types (System Config → Partnership
    /// Documents): certificates, awards, MoU/MoA, agreements and letters.
    /// Insert-by-name only — admin edits to fields, name or design are never
    /// overwritten, and removed types are not resurrected past their names.
    /// </summary>
    private static async Task SeedPartnerDocumentTypesAsync(OdinDbContext context, ILogger logger)
    {
        // One-time rename of the first-generation seed names onto the agreed
        // document list — keeps any design/field edits made under the old name.
        var renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Certificate of Partnership"] = "Partnership Certificate",
            ["Partnership Authorization Letter"] = "Authorization Letter",
        };
        var allTypes = await context.PartnerDocumentTypes.IgnoreQueryFilters().ToListAsync();
        var renamed = 0;
        foreach (var (oldName, newName) in renames)
        {
            var row = allTypes.FirstOrDefault(t =>
                string.Equals(t.Name, oldName, StringComparison.OrdinalIgnoreCase) && t.DeletedAt == null);
            if (row is null) continue;
            if (allTypes.Any(t => string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase))) continue;
            row.Name = newName;
            row.UpdatedAt = DateTime.UtcNow;
            renamed++;
        }

        var existingNames = allTypes.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Every starter places a "[school name]" tag, backed by a free-text
        // field so the Admission Office types the issuing school per document.
        var schoolField = new Letters.PartnerDocField("school-name", "School name", Letters.PartnerDocumentService.TextField, null);

        var added = 0;
        foreach (var (name, layout) in Letters.PartnerDocumentService.StarterTypes())
        {
            if (existingNames.Contains(name)) continue;
            context.PartnerDocumentTypes.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.PartnerDocumentType
            {
                Name = name,
                FieldsJson = Letters.PartnerDocumentService.SerializeFields([schoolField]),
                LayoutJson = layout,
            });
            added++;
        }
        if (added > 0 || renamed > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Partnership document types: +{Added} seeded, {Renamed} renamed", added, renamed);
        }
    }

    /// <summary>
    /// Retires the first-generation "Faculties"/"Teachers" datasheet seeds
    /// (superseded by the dedicated Faculties feature) when no partner ever
    /// used them.
    /// </summary>
    private static async Task SeedPartnerDatasheetTemplatesAsync(OdinDbContext context, ILogger logger)
    {
        var legacy = await context.PartnerDatasheetDefinitions
            .Where(d => d.DeletedAt == null
                && d.Scope != SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.PartnerDatasheetDefinition.ScopeFaculty
                && (d.Name == "Faculties" || d.Name == "Teachers"))
            .ToListAsync();
        var retired = 0;
        foreach (var d in legacy)
        {
            var inUse = await context.PartnerDatasheets.AnyAsync(s =>
                s.PartnerDatasheetDefinitionId == d.PartnerDatasheetDefinitionId && s.DeletedAt == null);
            if (inUse) continue;
            d.DeletedAt = DateTime.UtcNow;
            retired++;
        }
        if (retired > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Legacy faculty datasheet templates retired: {Count}", retired);
        }
    }

    /// <summary>
    /// Seeds the default Faculty Profile Information structure (the Faculties
    /// feature's OWN tables — not datasheets) with the agreed teacher field
    /// list. Runs only when the structure tables are completely empty, so an
    /// admin-edited (or migrated) structure is never overwritten.
    /// </summary>
    private static async Task SeedFacultyProfileStructureAsync(OdinDbContext context, ILogger logger)
    {
        if (await context.FacultyProfileSections.IgnoreQueryFilters().AnyAsync()) return;

        const string T = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeText;
        const string Sel = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeSelect;
        const string B = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeBool;
        const string F = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeFile;
        const string Auto = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeAutoId;
        const string Comp = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField.TypeComputed;

        var sections = new (string Title, string Kind, (string Label, string Type, string? Options, bool Required, bool PartnerEdit)[] Fields)[]
        {
            ("Teacher's information", SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileSection.KindFields, new[]
            {
                ("First name", T, (string?)null, true, true),
                ("Last name", T, null, true, true),
                ("Name", Comp, "{First name} {Last name}", false, false),
                ("Faculty ID", Auto, "MGW-ALC-FAC-{partner}-{n}", false, false),
                ("Faculty", T, null, false, true),
                ("School", T, null, false, false),
                ("Email", T, null, true, true),
                ("Position", Sel, "Professor\nSenior Lecturer\nLecturer\nTutor\nAssessment Marker", false, true),
                ("Main Discipline", T, null, false, true),
                ("Gender", Sel, "Male\nFemale\nAnother gender identity\nPrefer not to say", false, true),
                ("Teaching Programme", Sel, "Dip\nBBA\nMBA\nDBA", false, true),
                ("Highest Degree Achieved", T, null, false, true),
                ("Brief Bio", T, null, false, true),
                ("Approved by IBAS Academic Office", B, null, false, false),
                ("Faculty Academic Onboarding Programme", B, null, false, false),
                ("Employment Type", Sel, "Part-time\nFull-time", false, true),
                ("Status", Sel, "Active\nInactive", false, false),
                ("Nationality", T, null, false, true),
                ("Earned degrees (highest to lowest, with year obtained)", T, null, false, true),
                ("Professional qualifications (list 2 of: work experience, teaching excellence, professional certifications, additional coursework - last 5 years)", T, null, false, true),
            }),
            ("Teacher's profile - documents", SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileSection.KindFields, new[]
            {
                ("CV", F, (string?)null, false, true),
                ("Academic transcript", F, null, false, true),
                ("Academic certificate", F, null, false, true),
                ("Photo", F, null, false, true),
                ("Other documents", F, null, false, true),
                ("Documents are uploaded", B, null, false, false),
                ("Missing documents", Sel, "CV\nAcademic transcript\nAcademic certificate\nPhoto\nBrief Bio for public relation", false, false),
            }),
        };

        var sOrder = 0;
        foreach (var (title, kind, fields) in sections)
        {
            var section = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileSection
            {
                Title = title,
                Kind = kind,
                SortOrder = sOrder++,
            };
            context.FacultyProfileSections.Add(section);
            var fOrder = 0;
            foreach (var (label, type, options, required, partnerEdit) in fields)
            {
                context.FacultyProfileFields.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.FacultyProfileField
                {
                    FacultyProfileSectionId = section.FacultyProfileSectionId,
                    Label = label,
                    Type = type,
                    OptionsText = options,
                    IsRequired = required,
                    PartnerCanEdit = partnerEdit,
                    SortOrder = fOrder++,
                });
            }
        }
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Faculty profile structure seeded ({Count} sections)", sections.Length);
    }

    /// <summary>
    /// Seeds the five agreed Module Cohort upload fields (Teaching Materials
    /// + grading sheets). Runs only when the table is completely empty, so
    /// admin edits in the builder are never overwritten.
    /// </summary>
    private static async Task SeedCohortUploadFieldsAsync(OdinDbContext context, ILogger logger)
    {
        if (await context.CohortUploadFields.IgnoreQueryFilters().AnyAsync()) return;
        var defaultTypeId = await context.CohortTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => (Guid?)t.CohortTypeId)
            .FirstOrDefaultAsync();

        var seeds = new (string Label, bool Multiple, bool Grading, bool StudentVisible)[]
        {
            ("Teaching / Study Plan", false, false, false),
            ("Module Assessment Details", true, false, false),
            ("Module Teaching Materials", true, false, false),
            ("Grading Sheets & Rubrics", true, true, false),
            ("Module Outline Given to Students", false, false, true),
        };
        var order = 0;
        foreach (var (label, multiple, grading, studentVisible) in seeds)
        {
            context.CohortUploadFields.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortUploadField
            {
                CohortTypeId = defaultTypeId,
                Label = label,
                AllowMultiple = multiple,
                IsGradingSheet = grading,
                VisibleToStudents = studentVisible,
                SortOrder = order++,
            });
        }
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Module cohort upload fields seeded ({Count})", seeds.Length);
    }

    /// <summary>
    /// Seeds a default "Standard" cohort type (no extra fields) when none
    /// exist, and adopts legacy cohorts without a type onto it — a type is
    /// required on every cohort going forward.
    /// </summary>
    private static async Task SeedCohortTypesAsync(OdinDbContext context, ILogger logger)
    {
        var defaultType = await context.CohortTypes.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (defaultType is null)
        {
            defaultType = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortType { Name = "Standard" };
            context.CohortTypes.Add(defaultType);
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default cohort type seeded");
        }
        var orphans = await context.ModuleCohorts
            .Where(c => c.CohortTypeId == null)
            .ToListAsync();
        if (orphans.Count > 0)
        {
            foreach (var c in orphans) c.CohortTypeId = defaultType.CohortTypeId;
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] {Count} cohort(s) adopted onto the default cohort type", orphans.Count);
        }

        // Dissertation types (insert-by-name; admin edits never overwritten).
        var existingTypeNames = (await context.CohortTypes.IgnoreQueryFilters()
            .Select(t => t.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typesAdded = 0;
        void AddCohortType(string name,
            (string Label, string Type, string? Options, bool Required)[] dataFields,
            (string Label, bool Multiple, bool Grading, bool StudentVisible)[] uploads)
        {
            if (existingTypeNames.Contains(name)) return;
            var ctype = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortType { Name = name };
            context.CohortTypes.Add(ctype);
            var order = 0;
            foreach (var (label, type, options, required) in dataFields)
                context.CohortTypeFields.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortTypeField
                {
                    CohortTypeId = ctype.CohortTypeId,
                    Label = label, Type = type, OptionsText = options,
                    IsRequired = required, SortOrder = order++,
                });
            var uOrder = 0;
            foreach (var (label, multiple, grading, visible) in uploads)
                context.CohortUploadFields.Add(new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortUploadField
                {
                    CohortTypeId = ctype.CohortTypeId,
                    Label = label, AllowMultiple = multiple,
                    IsGradingSheet = grading, VisibleToStudents = visible,
                    SortOrder = uOrder++,
                });
            typesAdded++;
        }
        const string TSel = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortTypeField.TypeSelect;
        const string TBool = SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.CohortTypeField.TypeBool;
        AddCohortType("Dissertation Proposal",
            new[]
            {
                ("Supervisor documents uploaded", TBool, (string?)null, false),
                ("Supervisor school check", TSel, "Qualified\nUnqualified", false),
                ("School check — Marks", TSel, "Passed\nNot passed", false),
                ("School check — Format", TSel, "Passed\nNot passed", false),
            },
            new[]
            {
                ("Proposal documents", true, false, false),
                ("Supervisor's profile documents", true, false, false),
                ("Grading sheet for proposal", true, true, false),
            });
        AddCohortType("Final Project / Dissertation",
            new[]
            {
                ("School check — Marks", TSel, "Passed\nNot passed", false),
                ("School check — Format", TSel, "Passed\nNot passed", false),
            },
            new[]
            {
                ("Project documents", true, false, false),
                ("Grading sheet for project", true, true, false),
            });
        if (typesAdded > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Dissertation cohort types seeded: {Count}", typesAdded);
        }
    }

    /// <summary>
    /// One-time data fix: intake answers were briefly stored AES-encrypted at
    /// rest before the explicit decision (2026-07-15) that the intake feature
    /// carries no field encryption. Rows written in that window hold
    /// ciphertext; plaintext rows start with '{' or '[' and are skipped.
    /// Idempotent — once every row is plaintext there is nothing to do.
    /// </summary>
    private static async Task DecryptLegacyIntakeAnswersAsync(OdinDbContext context, ILogger logger)
    {
        var candidates = await context.IntakeResponses
            .Where(r => r.AnswersJson != "" && !r.AnswersJson.StartsWith("{") && !r.AnswersJson.StartsWith("["))
            .ToListAsync();
        if (candidates.Count == 0) return;

        var fixedCount = 0;
        foreach (var r in candidates)
        {
            try
            {
                r.AnswersJson = Odin.Api.Base.Crypto.FieldEncryption.DecryptString(r.AnswersJson);
                fixedCount++;
            }
            catch
            {
                // Not our ciphertext — leave the row untouched.
            }
        }
        if (fixedCount > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Intake responses: decrypted {Count} legacy encrypted answer payloads", fixedCount);
        }
    }

    /// <summary>
    /// Fills DocumentType.AiPrompt with a detailed verification prompt for
    /// the future AI document checker. Every prompt shares ONE strict output
    /// JSON schema (identical across all types) and adds type-specific
    /// expectations + fraud checks. Only rows with an empty prompt are
    /// touched, so admin-edited prompts are never overwritten.
    /// </summary>
    private static async Task SeedDocumentTypeAiPromptsAsync(OdinDbContext context, ILogger logger)
    {
        var types = await context.DocumentTypes.Where(d => d.DeletedAt == null).ToListAsync();
        var filled = 0;
        foreach (var t in types)
        {
            if (!string.IsNullOrWhiteSpace(t.AiPrompt)) continue;
            t.AiPrompt = BuildAiPrompt(t.Name);
            filled++;
        }
        // Schema upgrade: prompts from the first seeding lack the 0.00-1.00
        // confidence/fraudRisk fields the scan worker stores. Regenerate any
        // prompt still missing them (admin edits that kept the old schema
        // marker are regenerated too — acceptable this close to seeding).
        foreach (var t in types)
        {
            if (t.AiPrompt != null && t.AiPrompt.Contains("recommendedAction") && !t.AiPrompt.Contains("fraudRisk"))
            {
                t.AiPrompt = BuildAiPrompt(t.Name);
                filled++;
            }
        }
        if (filled > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Document types: seeded AI verification prompts for {Count} types", filled);
        }
    }

    private static string BuildAiPrompt(string typeName)
    {
        var n = typeName.ToLowerInvariant();
        string specifics;
        if (n.Contains("passport"))
            specifics = "EXPECTED CHARACTERISTICS: A government-issued passport identity page. Look for: the machine-readable zone (MRZ, two lines of 44 characters with << filler); a portrait photo; document number, issuing state, nationality, date of birth, sex, issue/expiry dates; security printing (guilloche patterns, microtext). VALIDATION: recompute the MRZ check digits for document number, date of birth and expiry and compare them with the printed check digits; verify the printed fields match the MRZ fields exactly; verify the expiry date is in the future. FRAUD CHECKS: photo edges that look pasted or re-compressed differently from the page; font or kerning changes inside a single field; MRZ check-digit failures; issue/expiry ranges that do not match the issuing country's validity rules (usually 5 or 10 years); a date of birth inconsistent with the applicant's stated age.";
        else if (n.Contains("birth"))
            specifics = "EXPECTED CHARACTERISTICS: An official civil birth registration document. Look for: an issuing civil authority (registry office, municipality, ministry), a registration number, full name of the child, date and place of birth, parents' names, an official stamp/seal and a signature or digital verification code. FRAUD CHECKS: missing registration number or authority; fonts inconsistent with the rest of the form; a stamp that is perfectly uniform (copy-pasted) or overlaps text unnaturally; a date of birth that conflicts with other documents in the application.";
        else if (n.Contains("language"))
            specifics = "EXPECTED CHARACTERISTICS: A recognised language test result (IELTS, TOEFL, Duolingo English Test, Cambridge, PTE or equivalent). Look for: the testing organisation's branding, a candidate/test-report number, test date, per-skill scores (listening/reading/writing/speaking) and an overall score/band consistent with the per-skill scores, and the candidate's name and often photo or date of birth. VALIDATION: check the overall score is mathematically consistent with the sub-scores for the claimed test; check the score scale matches the test (e.g. IELTS 0-9 in 0.5 steps, TOEFL iBT 0-120). FRAUD CHECKS: scores outside the test's scale or granularity; test date older than the test's validity window (usually 2 years); mismatched fonts around the scores; a report number format that does not match the organisation's format.";
        else if (n.Contains("curriculum") || n == "cv")
            specifics = "EXPECTED CHARACTERISTICS: A curriculum vitae / resume: personal details, chronological education history and employment history, possibly skills and references. This is a self-authored document, so authenticity of layout is NOT a fraud signal. VALIDATION: check internal chronology (no overlapping impossible periods, education ages plausible), and that the stated qualifications are consistent with the other documents in the application. FRAUD CHECKS: impossible or reversed date ranges; degrees claimed from institutions that do not exist; a work history that contradicts the applicant's date of birth.";
        else if (n.Contains("motivation"))
            specifics = "EXPECTED CHARACTERISTICS: A personal statement / letter of motivation authored by the applicant explaining why they pursue the programme. Self-authored: layout carries no authenticity weight. VALIDATION: confirm it is actually a motivation letter (first-person prose about study intentions), reasonably specific to a programme, and not an unrelated document. FRAUD CHECKS: text plagiarised from well-known templates verbatim; a letter clearly written for a different institution or programme; contradictions with the applicant's stated background.";
        else if (n.Contains("card picture") || n.Contains("photo"))
            specifics = "EXPECTED CHARACTERISTICS: A portrait photograph of one person suitable for an ID card: face clearly visible, roughly frontal, neutral background preferred, no sunglasses or heavy obstruction, adequate resolution and lighting. VALIDATION: exactly one face; face occupies a reasonable share of the frame; image sharp enough to print at ID size. FRAUD CHECKS: signs of AI generation or heavy retouching (waxy skin, asymmetric artefacts, inconsistent lighting between face and background); a photo of a photo/screen (moiré patterns, screen bezels); celebrity or stock imagery.";
        else if (n.Contains("transcript"))
            specifics = "EXPECTED CHARACTERISTICS: An academic transcript / grade report: issuing institution header, student name and ID, programme name, a table of courses/modules with credits (often ECTS) and grades, totals or GPA, issue date, and a stamp/signature or verification code. VALIDATION: recompute totals - credit sums and GPA/averages must match the listed rows; grades must lie within the printed grading scale; the award level must match this document type. FRAUD CHECKS: rows whose font, spacing or baseline differ from the rest of the table (row insertion); arithmetic that does not add up; a grading legend inconsistent with the grades used; an institution name/logo mismatch; issue dates before the covered study period ends.";
        else if (n.Contains("certificate") || n.Contains("diploma") || n.Contains("degree") || n.Contains("doctorate"))
            specifics = "EXPECTED CHARACTERISTICS: An award certificate: issuing institution name and branding, the holder's full name, the exact qualification awarded, award/conferral date, signatures of officials and an institutional seal/stamp, often a certificate number or verification URL. VALIDATION: the qualification level printed on the document must match this expected document type (do not accept a diploma where a bachelor's degree is expected); the award date must be plausible relative to the applicant's age and other documents. FRAUD CHECKS: name or qualification text in a different font, size or ink density than surrounding text (overlay editing); pixelation or compression halos around the holder's name; seals/signatures identical to templates found on diploma-mill designs; institutions that cannot be verified to exist; conferral dates on weekends/holidays inconsistent with the institution's practice.";
        else if (n.Contains("letter"))
            specifics = "EXPECTED CHARACTERISTICS: An official institutional letter (offer/admission or similar): letterhead with institution branding and contact details, addressee, a reference number, an issue date, body text stating the decision or purpose, and an authorised signature. FRAUD CHECKS: letterhead artwork at different resolution than the text layer; reference numbers that do not match the institution's format; signature images reused pixel-identically from other documents; dates inconsistent with the described process.";
        else
            specifics = "EXPECTED CHARACTERISTICS: Judge against the document type name and description. Identify the issuing party, the subject person, key dates and identifiers. FRAUD CHECKS: inconsistent fonts or alignment within fields, compression artefacts localised around names/dates/numbers, missing issuer identification, internally contradictory dates.";

        return
"You are a meticulous document verification analyst for a university admissions office. " +
"You are given ONE uploaded document (image or PDF pages) that the applicant claims is of type: \"" + typeName + "\".\n\n" +
"TASKS - perform ALL of them:\n" +
"1. CLASSIFY: determine what the document actually is, from its content alone.\n" +
"2. MATCH: decide whether it genuinely is a \"" + typeName + "\" and state how confident you are (0-100).\n" +
"3. EXTRACT: pull out the key fields (holder name, issuer, dates, identifying numbers, and the fields listed below).\n" +
"4. VALIDATE: run the type-specific validation rules below; every failed rule lowers legitimacy.\n" +
"5. FRAUD ANALYSIS: examine typography consistency, alignment, compression/retouch artefacts around critical fields, seal/signature plausibility, internal arithmetic and date logic, and cross-field consistency. List every indicator you find with a severity.\n" +
"6. Be conservative: if the image is too blurry, cropped or incomplete to judge, say so via legitimacy=\"unreadable\" rather than guessing.\n\n" +
specifics + "\n\n" +
"OUTPUT - respond with ONLY this JSON object, no prose, no markdown fences. The schema is identical for every document type:\n" +
"{\n" +
"  \"documentTypeExpected\": \"" + typeName + "\",\n" +
"  \"detectedType\": \"<what the document actually appears to be>\",\n" +
"  \"matchesExpectedType\": true,\n" +
"  \"typeConfidence\": 0,\n" +
"  \"legitimacy\": \"legitimate | suspicious | likely_fraudulent | unreadable\",\n" +
"  \"confidence\": 0.00,\n" +
"  \"fraudRisk\": 0.00,\n" +
"  \"legitimacyConfidence\": 0,\n" +
"  \"extracted\": { \"holderName\": \"\", \"issuer\": \"\", \"issueDate\": \"\", \"expiryDate\": \"\", \"documentNumber\": \"\", \"otherKeyFields\": {} },\n" +
"  \"validationChecks\": [ { \"check\": \"\", \"passed\": true, \"detail\": \"\" } ],\n" +
"  \"fraudIndicators\": [ { \"indicator\": \"\", \"severity\": \"low | medium | high\", \"detail\": \"\" } ],\n" +
"  \"qualityIssues\": [ \"\" ],\n" +
"  \"summary\": \"<2-3 sentences for the admissions officer>\",\n" +
"  \"recommendedAction\": \"approve | manual_review | reject\"\n" +
"}\n\n" +
"SCORING RULES: typeConfidence and legitimacyConfidence are integers 0-100. confidence and fraudRisk are decimals from 0.00 to 1.00: confidence = your overall certainty that this document is genuine AND of the expected type; fraudRisk = the probability it is forged or manipulated. recommendedAction must be \"reject\" if any high-severity fraud indicator exists or the type does not match; \"manual_review\" for medium indicators, low confidence (<70) or unreadable input; \"approve\" only when the type matches with confidence >= 85 and no medium/high indicators exist.";
    }

}
