using System.Security.Cryptography;

namespace Odin.Api.Base.Crypto;

public static class CryptoUtils
{
    public static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
