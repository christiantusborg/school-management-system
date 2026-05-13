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
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        await SeedSystemLetterAssetsAsync(context, fileStorage, logger);
        await SeedDefaultAdmissionLetterAsync(context, logger);
        await SeedDefaultOfferLetterAsync(context, logger);
        await SeedDefaultTranscriptAsync(context, logger);
        await SeedDefaultCertificateAsync(context, logger);
        await SeedDefaultProvisionalCertificateAsync(context, logger);
        await SeedDefaultLetterLayoutsAsync(context, logger);
        var eduLevelByName = await SeedEducationLevelsAsync(context, logger);
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
    /// Inserts the IBSS default <c>AdmissionLetter</c> body into every
    /// existing <see cref="Programme"/> that does not already have one.
    /// Never overwrites a row admins have edited.
    /// </summary>
    private static async Task SeedDefaultAdmissionLetterAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var alreadyHave = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.AdmissionLetter && t.DeletedAt == null)
            .Select(t => t.ProgrammeId)
            .ToListAsync();
        var alreadySet = alreadyHave.ToHashSet();

        var html = DefaultAdmissionLetterHtml;
        var added = 0;
        foreach (var pid in programmeIds)
        {
            if (alreadySet.Contains(pid)) continue;
            context.LetterTemplates.Add(new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pid,
                LetterType = LetterType.AdmissionLetter,
                BodyHtml = html,
                UpdatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default admission letter: +{Count} programmes seeded", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Default admission letter: every programme already has one — skipping");
        }
    }

    /// <summary>
    /// Inserts the IBSS default <c>OfferLetter</c> body into every existing
    /// <see cref="Programme"/> that does not already have one. Idempotent.
    /// </summary>
    private static async Task SeedDefaultOfferLetterAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var alreadyHave = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.OfferLetter && t.DeletedAt == null)
            .Select(t => t.ProgrammeId)
            .ToListAsync();
        var alreadySet = alreadyHave.ToHashSet();

        var html = DefaultOfferLetterHtml;
        var added = 0;
        foreach (var pid in programmeIds)
        {
            if (alreadySet.Contains(pid)) continue;
            context.LetterTemplates.Add(new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pid,
                LetterType = LetterType.OfferLetter,
                BodyHtml = html,
                UpdatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default offer letter: +{Count} programmes seeded", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Default offer letter: every programme already has one — skipping");
        }
    }

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
                    var existing = CertificateLayout.TryParse(row.CertificateLayoutJson);
                    var existingFp = existing?.SeedFingerprint ?? 0;
                    if (existingFp < DefaultLetterLayouts.CurrentTranscriptFingerprint)
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
    private static async Task SeedDefaultCertificateAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var alreadyHave = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.Certificate && t.DeletedAt == null)
            .Select(t => t.ProgrammeId)
            .ToListAsync();
        var alreadySet = alreadyHave.ToHashSet();

        var layoutJson = DefaultCertificateLayoutJson;
        var added = 0;
        foreach (var pid in programmeIds)
        {
            if (alreadySet.Contains(pid)) continue;
            context.LetterTemplates.Add(new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pid,
                LetterType = LetterType.Certificate,
                BodyHtml = null,
                CertificateLayoutJson = layoutJson,
                UpdatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default certificate layout: +{Count} programmes seeded", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Default certificate layout: every programme already has one — skipping");
        }
    }

    /// <summary>
    /// Seeds a starting layout for the Provisional Certificate (the
    /// stamp-and-signature-free variant). Reuses the standard certificate
    /// JSON so admins get the same field placement to start from; they swap
    /// the background image / remove signature fields per programme. Idempotent.
    /// </summary>
    private static async Task SeedDefaultProvisionalCertificateAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var alreadyHave = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.ProvisionalCertificate && t.DeletedAt == null)
            .Select(t => t.ProgrammeId)
            .ToListAsync();
        var alreadySet = alreadyHave.ToHashSet();

        var layoutJson = DefaultCertificateLayoutJson;
        var added = 0;
        foreach (var pid in programmeIds)
        {
            if (alreadySet.Contains(pid)) continue;
            context.LetterTemplates.Add(new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pid,
                LetterType = LetterType.ProvisionalCertificate,
                BodyHtml = null,
                CertificateLayoutJson = layoutJson,
                UpdatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default provisional certificate layout: +{Count} programmes seeded", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Default provisional certificate layout: every programme already has one — skipping");
        }
    }

    private static string DefaultCertificateLayoutJson { get; } = $@"{{
  ""backgroundAssetId"": ""{SystemLetterAssetIds.IbssCertificateBg}"",
  ""width"": 2000,
  ""height"": 1414,
  ""fields"": [
    {{ ""id"": ""studentId"",      ""tag"": ""[student number]"",      ""prefix"": ""Student ID: "", ""x"": 1900, ""y"": 130, ""fontSize"": 28, ""color"": ""#000000"", ""align"": ""right"",  ""bold"": true }},
    {{ ""id"": ""awardsTo"",       ""text"": ""International Business School of Scandinavia Hereby Awards To"", ""x"": 0, ""y"": 360, ""fontSize"": 28, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""studentName"",    ""tag"": ""[student full name]"",   ""x"": 0, ""y"": 430, ""fontSize"": 52, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""whoSatisfied"",   ""text"": ""Who has satisfactorily completed the studies prescribed and therefore has been granted the degree of"", ""x"": 0, ""y"": 530, ""fontSize"": 24, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""programmeName"",  ""tag"": ""[program name]"",        ""x"": 0, ""y"": 600, ""fontSize"": 38, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""withSpec"",       ""text"": ""With a specialisation in"", ""x"": 0, ""y"": 690, ""fontSize"": 24, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""specName"",       ""tag"": ""[specialization name]"", ""x"": 0, ""y"": 760, ""fontSize"": 34, ""color"": ""#A6862F"", ""align"": ""center"", ""bold"": true }},
    {{ ""id"": ""witnessLine1"",   ""text"": ""With all its right and privileges in the witness whereof the seal of the"", ""x"": 0, ""y"": 850, ""fontSize"": 22, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""witnessLine2"",   ""text"": ""International Business School of Scandinavia is hereunto affixed."", ""x"": 0, ""y"": 890, ""fontSize"": 22, ""color"": ""#1a2d4f"", ""align"": ""center"", ""italic"": true }},
    {{ ""id"": ""presentedOn"",    ""tag"": ""[graduation date]"",     ""prefix"": ""Presented on "", ""suffix"": "" in Denmark."", ""x"": 0, ""y"": 950, ""fontSize"": 24, ""color"": ""#000000"", ""align"": ""center"", ""bold"": true }}
  ]
}}";

    /// <summary>
    /// Inserts the IBSS default <c>Transcript</c> body into every existing
    /// <see cref="Programme"/> that does not already have one. Idempotent.
    /// </summary>
    private static async Task SeedDefaultTranscriptAsync(OdinDbContext context, ILogger logger)
    {
        var programmeIds = await context.Programmes
            .Where(p => p.DeletedAt == null)
            .Select(p => p.ProgrammeId)
            .ToListAsync();
        if (programmeIds.Count == 0) return;

        var alreadyHave = await context.LetterTemplates
            .Where(t => t.LetterType == LetterType.Transcript && t.DeletedAt == null)
            .Select(t => t.ProgrammeId)
            .ToListAsync();
        var alreadySet = alreadyHave.ToHashSet();

        var html = DefaultTranscriptHtml;
        var added = 0;
        foreach (var pid in programmeIds)
        {
            if (alreadySet.Contains(pid)) continue;
            context.LetterTemplates.Add(new LetterTemplate
            {
                LetterTemplateId = Guid.NewGuid(),
                ProgrammeId = pid,
                LetterType = LetterType.Transcript,
                BodyHtml = html,
                UpdatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("[Seeder] Default transcript: +{Count} programmes seeded", added);
        }
        else
        {
            logger.LogInformation("[Seeder] Default transcript: every programme already has one — skipping");
        }
    }

    private static string DefaultTranscriptHtml { get; } = $@"
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""IBSS"" /></p>
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
<tr><th>IBSS Grade</th><th>UK Grade</th><th>US Grade</th><th>ECTS Grade</th><th>ECTS Grade Points</th><th>Remark</th></tr>
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
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""IBSS"" /></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSecondaryLogo}"" alt="""" /></p>
<h2 style=""text-align:center;"">Offer Letter</h2>
<p>Date: [date]</p>
<p>Ref: </p>
<p>Name: <strong>[student full name]</strong></p>
<p>Passport/ID No.: <strong>[passport id]</strong></p>
<p>Address: <strong>[student address]</strong></p>
<p></p>
<p>Dear <strong>[student full name]</strong>,</p>
<p>Congratulations. We are pleased to inform you that your application for <strong>International Business School of Scandinavia (IBSS)</strong> is approved. We look forward to having you with us. Our records for your admission will carry the following information:</p>
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
<p><strong>International Business School of Scandinavia (IBSS)</strong> would like to congratulate you to join the programme in your quest towards academic and career advancement.</p>
<p>We wish you every success!</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssStamp}"" alt=""IBSS Stamp"" /></p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssSignatureLine}"" alt=""Signature Line"" /></p>
<p>Anna Phan</p>
<p>Head of Administration</p>
<p></p>
<h3>(Please fill up this part)</h3>
<h3>Applicant's Confirmation</h3>
<p>By paying the tuition fee of the program, I, <strong>[student full name]</strong>, <strong>[passport id]</strong> accept the offer to study <strong>[program name]</strong> in International Business School of Scandinavia (IBSS). I hereby acknowledge that I have read and understand the terms and conditions of this offer letter and on the website (<a href=""https://ibss.edu.eu/"">https://ibss.edu.eu/</a>).</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssFooter}"" alt=""Footer"" /></p>
";

    private static string DefaultAdmissionLetterHtml { get; } = $@"
<p><img data-asset-id=""{SystemLetterAssetIds.IbssLogo}"" alt=""IBSS"" /></p>
<p>Date: [date]</p>
<p>Ref: </p>
<h2 style=""text-align:center;"">Admission Letter</h2>
<p>Name: <strong>[student full name]</strong></p>
<p>Passport/ID No.: <strong>[passport id]</strong></p>
<p>Address: <strong>[student address]</strong></p>
<p></p>
<p>Dear <strong>[student full name]</strong>,</p>
<p><strong>International Business School of Scandinavia (IBSS)</strong> would like to take this opportunity to congratulate and welcome you to the programme in your quest towards academic and career advancement. It is our pleasure that you have been accepted into the programme.</p>
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
<p>Participation in this programme is governed by IBSS Terms &amp; Conditions (see <a href=""http://ibss.edu.eu/"">http://ibss.edu.eu/</a>).</p>
<p><img data-asset-id=""{SystemLetterAssetIds.IbssStamp}"" alt=""IBSS Stamp"" /></p>
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
                IsSystemGenerated = true,
            });
            added++;
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
        var pathwayByName = existingPathways
            .ToDictionary(p => p.Name.Trim().ToLowerInvariant(), p => p);

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
}
