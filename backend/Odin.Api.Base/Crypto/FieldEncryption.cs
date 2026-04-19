using System.Security.Cryptography;

namespace Odin.Api.Base.Crypto;

/// <summary>
/// Transparent AES-256-GCM field-level encryption for EF value converters.
/// </summary>
public static class FieldEncryption
{
    private static byte[]? _key;

    public static void Configure(string hexKey)
    {
        if (hexKey.Length != 64)
            throw new ArgumentException("FieldKey must be a 64-character hex string (32 bytes).", nameof(hexKey));
        _key = Convert.FromHexString(hexKey);
    }

    private static byte[] Key => _key ?? throw new InvalidOperationException(
        "FieldEncryption not configured. Call FieldEncryption.Configure() at startup.");

    public static byte[] Encrypt(byte[] plaintext)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(Key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);
        return result;
    }

    public static byte[] Decrypt(byte[] encrypted)
    {
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = encrypted[..nonceSize];
        var tag = encrypted[nonceSize..(nonceSize + tagSize)];
        var ciphertext = encrypted[(nonceSize + tagSize)..];

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(Key, tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static string EncryptString(string plaintext)
        => Convert.ToBase64String(Encrypt(System.Text.Encoding.UTF8.GetBytes(plaintext)));

    public static string DecryptString(string encrypted)
        => System.Text.Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(encrypted)));
}
