using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

/// <summary>
/// Provides an extension method to convert a string into a deterministic <see cref="Guid"/>.
/// </summary>
public static class StringExtensionDeterministicGuid
{
    /// <summary>
    /// Converts the specified <paramref name="input"/> string into a deterministic <see cref="Guid"/>
    /// using MD5 hashing.
    /// </summary>
    /// <param name="input">The input string to be converted.</param>
    /// <returns>A deterministic <see cref="Guid"/> representation of the input string.</returns>
    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification = "MD5 is used solely for creating a hash, not for security purposes.")]
    [SuppressMessage("Performance", "CA1850:Prefer static 'HashData' method over 'ComputeHash'")]
    public static Guid AsDeterministicGuid(this string input)
    {
        Debug.Assert(!string.IsNullOrEmpty(input), nameof(input) + " cannot be null or empty.");

        using var md5Hasher = MD5.Create();
        Span<byte> hashBytes = stackalloc byte[16];
        md5Hasher.TryComputeHash(Encoding.UTF8.GetBytes(input), hashBytes, out _);

        return new Guid(hashBytes);
    }
}
