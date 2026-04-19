using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Odin.Api.Base.Crypto;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using System.Security.Cryptography;

namespace Odin.Api.Base.Authentication;

public class SessionTokenService(OdinDbContext db, IConfiguration config)
{
    public async Task<(string RawToken, SessionToken Session)> CreateTokenAsync(
        string userId, string? deviceInfo = null, CancellationToken cancellationToken = default)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = CryptoUtils.HashToken(rawToken);
        var expirationDays = config.GetValue("TokenSettings:ExpirationDays", 30);

        var session = new SessionToken
        {
            TokenHash = hash,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            DeviceInfo = deviceInfo
        };

        db.SessionTokens.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return (rawToken, session);
    }

    public async Task<SessionToken?> ValidateTokenAsync(string rawToken)
    {
        var hash = CryptoUtils.HashToken(rawToken);
        return await db.SessionTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == hash &&
                t.RevokedAt == null &&
                t.ExpiresAt > DateTime.UtcNow &&
                t.User.IsEnabled);
    }

    public async Task RevokeTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = CryptoUtils.HashToken(rawToken);
        var token = await db.SessionTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.SessionTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
