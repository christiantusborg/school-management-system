using System.Security.Cryptography;

namespace QuVian.SharedLibrary.Basics.Extensions.MemoryStreams.Cryptography;

/// <summary>
/// Provides methods for performing AES encryption and decryption on MemoryStream objects.
/// </summary>
public static class AesEncryptionHelper
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public static bool TryEncrypt(this MemoryStream plainStream, string password, byte[]? salt,
        out MemoryStream? encryptedStream)
    {
        encryptedStream = null;

        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Password cannot be null or empty.");
            return false;
        }

        if (salt is not { Length: 16, })
        {
            Console.WriteLine("Salt must be exactly 16 bytes long.");
            return false;
        }

        try
        {
            var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(32); // AES-256 requires a 256-bit key
            var iv = deriveBytes.GetBytes(16); // AES block size is 128 bits (16 bytes)

            plainStream.Seek(0, SeekOrigin.Begin);
            var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = iv;

            encryptedStream = new MemoryStream();
            var cryptoStream = new CryptoStream(encryptedStream, aesAlg.CreateEncryptor(), CryptoStreamMode.Write,
                leaveOpen: true);
            plainStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
            encryptedStream.Seek(0, SeekOrigin.Begin);
            return true;
        }
        catch (CryptographicException e)
        {
            Console.WriteLine($"Encryption failed due to cryptographic error: {e.Message}");
            return false;
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"Encryption failed due to an invalid argument: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occurred during encryption: {e.Message}");
            return false;
        }
    }

    // Decrypts a MemoryStream
    /// Attempts to decrypt an encrypted MemoryStream using the specified password and salt.
    /// The method returns a boolean indicating the success of the operation. If successful,
    /// the output parameter `decryptedStream` will contain the decrypted MemoryStream.
    /// <param name="encryptedStream">
    /// The MemoryStream containing the encrypted data. Must not be null.
    /// </param>
    /// <param name="password">
    /// The password used for decryption. Must not be null or empty.
    /// </param>
    /// <param name="salt">
    /// The salt used for key derivation during decryption. Must be exactly 16 bytes long.
    /// </param>
    /// <param name="decryptedStream">
    /// If the method returns true, this parameter will contain the decrypted MemoryStream.
    /// If the decryption fails, this will be null.
    /// </param>
    /// <returns>
    /// True if the decryption is successful; otherwise, false.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public static bool TryDecrypt(this MemoryStream? encryptedStream, string password, byte[]? salt,
        out MemoryStream? decryptedStream)
    {
        decryptedStream = null;

        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Password cannot be null or empty.");
            return false;
        }

        if (salt == null || salt.Length != 16)
        {
            Console.WriteLine("Salt must be exactly 16 bytes long.");
            return false;
        }

        try
        {
            // Specify the hash algorithm and the iteration count explicitly
            var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(32); // AES-256 requires a 256-bit key
            var iv = deriveBytes.GetBytes(16); // AES block size is 128 bits (16 bytes)

            encryptedStream!.Seek(0, SeekOrigin.Begin);
            var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = iv;

            decryptedStream = new MemoryStream();
            var cryptoStream = new CryptoStream(encryptedStream, aesAlg.CreateDecryptor(), CryptoStreamMode.Read,
                leaveOpen: true);
            cryptoStream.CopyTo(decryptedStream);
            decryptedStream.Seek(0, SeekOrigin.Begin);
            return true;
        }
        catch (CryptographicException e)
        {
            Console.WriteLine($"Decryption failed due to cryptographic error: {e.Message}");
            return false;
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"Decryption failed due to an invalid argument: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"An unexpected error occurred during decryption: {e.Message}");
            return false;
        }
    }
}