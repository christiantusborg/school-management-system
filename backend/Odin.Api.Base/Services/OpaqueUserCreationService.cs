using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Services;

public class OpaqueUserCreationService(
    UserManager<ApplicationUser> userManager,
    OdinDbContext dbContext,
    ILogger<OpaqueUserCreationService> logger)
{
    public string GeneratePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$%&*";
        const string all     = upper + lower + digits + special;

        var rng = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(rng);
        var chars = new char[16];
        chars[0]  = upper[rng[0]  % upper.Length];
        chars[1]  = lower[rng[1]  % lower.Length];
        chars[2]  = digits[rng[2] % digits.Length];
        chars[3]  = special[rng[3] % special.Length];
        for (int i = 4; i < 16; i++)
            chars[i] = all[rng[i] % all.Length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(rng);
        for (int i = 15; i > 0; i--)
        {
            int j = rng[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    public async Task<(byte[] OprfSeed, byte[] ClientPublicKey, byte[] KemPublicKey, byte[] KemEncryptedPrivKey, byte[] KemNonce)>
        ComputeCredentials(string password)
    {
        var webProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Odin.Web"));
        var scriptPath    = Path.Combine(webProjectDir, "scripts", "compute-opaque-credentials.mjs");

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"OPAQUE credential script not found: {scriptPath}");

        var psi = new ProcessStartInfo("node", $"\"{scriptPath}\" \"{password}\"")
        {
            WorkingDirectory       = webProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error  = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger.LogError("compute-opaque-credentials failed: {Error}", error);
            throw new InvalidOperationException($"Credential generation failed: {error}");
        }

        var json          = JsonDocument.Parse(output);
        var oprfSeed      = Convert.FromBase64String(json.RootElement.GetProperty("oprfSeed").GetString()!);
        var clientPubKey  = Convert.FromBase64String(json.RootElement.GetProperty("clientPublicKey").GetString()!);
        var kemPubKey     = Convert.FromBase64String(json.RootElement.GetProperty("kemPublicKey").GetString()!);
        var kemEncPrivKey = Convert.FromBase64String(json.RootElement.GetProperty("kemEncryptedPrivKey").GetString()!);
        var kemNonce      = Convert.FromBase64String(json.RootElement.GetProperty("kemNonce").GetString()!);

        return (oprfSeed, clientPubKey, kemPubKey, kemEncPrivKey, kemNonce);
    }

    /// <summary>
    /// Creates an ApplicationUser with OPAQUE credentials, assigns role and optional PartnerId.
    /// Returns the generated password.
    /// </summary>
    public async Task<(ApplicationUser User, string Password)> CreateUserAsync(
        string username,
        string email,
        string role,
        Guid? partnerId = null,
        CancellationToken ct = default,
        string? customPassword = null)
    {
        if (await userManager.FindByNameAsync(username) is not null)
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        // Use admin-supplied password if provided; otherwise generate one.
        // Trim to defend against whitespace mistakes from the wizard input.
        var password    = string.IsNullOrWhiteSpace(customPassword) ? GeneratePassword() : customPassword.Trim();
        var credentials = await ComputeCredentials(password);

        var user = new ApplicationUser
        {
            UserName               = username,
            Email                  = email,
            EmailConfirmed         = true,
            IsEnabled              = true,
            RecoveryCodesConfirmed = true,
            PartnerId              = partnerId,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);

        dbContext.OpaqueCredentials.Add(new OpaqueCredential
        {
            UserId          = user.Id,
            OprfSeed        = credentials.OprfSeed,
            ClientPublicKey = credentials.ClientPublicKey
        });

        dbContext.KemKeyPairs.Add(new KemKeyPair
        {
            UserId              = user.Id,
            PublicKey           = credentials.KemPublicKey,
            EncryptedPrivateKey = credentials.KemEncryptedPrivKey,
            Nonce               = credentials.KemNonce
        });

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Created user '{Username}' with role '{Role}'", username, role);
        return (user, password);
    }

    /// <summary>
    /// Generates a fresh password, recomputes OPAQUE credentials for the given user and
    /// inserts new OpaqueCredential + KemKeyPair rows. Caller is responsible for removing
    /// the existing credential / KEM rows before invoking.
    /// Returns the plaintext password for one-time display to the admin.
    /// </summary>
    public async Task<string> RegenerateCredentialsAsync(ApplicationUser user, CancellationToken ct = default, string? customPassword = null)
    {
        var password    = string.IsNullOrWhiteSpace(customPassword) ? GeneratePassword() : customPassword.Trim();
        var credentials = await ComputeCredentials(password);

        dbContext.OpaqueCredentials.Add(new OpaqueCredential
        {
            UserId          = user.Id,
            OprfSeed        = credentials.OprfSeed,
            ClientPublicKey = credentials.ClientPublicKey
        });

        dbContext.KemKeyPairs.Add(new KemKeyPair
        {
            UserId              = user.Id,
            PublicKey           = credentials.KemPublicKey,
            EncryptedPrivateKey = credentials.KemEncryptedPrivKey,
            Nonce               = credentials.KemNonce
        });

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Regenerated OPAQUE credentials for user '{Username}'", user.UserName);
        return password;
    }
}
