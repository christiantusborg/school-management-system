using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data;

public static class DatabaseSeeder
{
    private record SeedUser(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string OutputFile);

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context     = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger      = scope.ServiceProvider.GetRequiredService<ILogger<OdinDbContext>>();

        await context.Database.MigrateAsync();

        // ── Roles ─────────────────────────────────────────────────────────────
        string[] roles = ["Admin", "Partner", "Student", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }

        // ── Seed users ────────────────────────────────────────────────────────
        var seedUsers = new List<SeedUser>
        {
            new("admin", "admin@odin.local",   "System", "Administrator", "Admin", ""),
            new("adm",   "adm@terbium.dk",     "Admin",  "User",          "Admin", "adm.txt"),
            new("ctu",   "ctu@terbium.dk",     "CTU",    "User",          "User",  "ctu.txt"),
            new("ict",   "ict@terbium.dk",     "ICT",    "User",          "User",  "ict.txt"),
        };

        foreach (var seed in seedUsers)
        {
            if (await userManager.FindByNameAsync(seed.Username) is not null)
            {
                logger.LogInformation("[Seeder] User '{Username}' already exists, skipping", seed.Username);
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
