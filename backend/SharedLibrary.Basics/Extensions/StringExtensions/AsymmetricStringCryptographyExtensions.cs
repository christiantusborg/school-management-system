using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

/// <summary>
/// Provides extension methods for encrypting and decrypting strings using RSA cryptography.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public static class AsymmetricStringCryptographyExtensions
{
    public static bool AsymmetricEncrypt(this string plainText, Type publicKey, out string encryptedText)
    {
        return AsymmetricEncrypt(plainText, publicKey.Name, out encryptedText);
    }

    /// <summary>
    /// Tries to encrypt the provided plain text using the specified public key.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="publicKey">The public key used for encryption.</param>
    /// <param name="encryptedText">The encrypted text output parameter.</param>
    /// <returns>
    /// Returns true if the encryption is successful; otherwise, false.
    /// </returns>
    public static bool AsymmetricEncrypt(this string plainText, string publicKey, out string encryptedText)
    {
        encryptedText = string.Empty;

        if (string.IsNullOrEmpty(plainText))
        {
            Console.WriteLine("Encryption_failed_Input_plainText_cannot_be_null_or_empty.");
            return false;
        }

        if (string.IsNullOrEmpty(publicKey))
        {
            Console.WriteLine("Encryption_failed_Input_publicKey_cannot_be_null_or_empty.");
            return false;
        }

        try
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);  // Import the public key
            var dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = rsa.Encrypt(dataToEncrypt, false);
            encryptedText = Convert.ToBase64String(encryptedData);
            return true;

        }
        catch (CryptographicException ce)
        {
            Console.WriteLine($"Decryption error: Cryptographic operation failed - {ce.Message}");
            return false;
        }
        catch (FormatException fe)
        {
            Console.WriteLine($"Decryption error: Input format is incorrect - {fe.Message}");
            return false;
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine($"Decryption error: Invalid argument - {ae.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decryption error: General exception - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to decrypt the given encrypted text using the provided private key
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt</param>
    /// <param name="privateKey">The private key to use for decryption</param>
    /// <param name="decryptedText">The decrypted text (output parameter)</param>
    /// <returns>
    /// True if the decryption was successful and the decrypted text is returned in the <paramref name="decryptedText"/> parameter,
    /// False if the decryption failed and no decrypted text is returned
    /// </returns>
    public static bool AsymmetricDecrypt(this string encryptedText, string privateKey, out string decryptedText)
    {
        decryptedText = string.Empty;

        if (string.IsNullOrEmpty(encryptedText))
        {
            Console.WriteLine("Encryption failed Input encryptedText cannot be null or empty.");
            return false;
        }

        if (string.IsNullOrEmpty(privateKey))
        {
            Console.WriteLine("Encryption failed Input privateKey cannot be null or empty.");
            return false;
        }

        try
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);  // Import the private key
            var dataToDecrypt = Convert.FromBase64String(encryptedText);
            var decryptedData = rsa.Decrypt(dataToDecrypt, false);
            decryptedText = Encoding.UTF8.GetString(decryptedData);
            return true;

        }
        catch (CryptographicException ce)
        {
            Console.WriteLine($"Decryption error: Cryptographic operation failed - {ce.Message}");
            return false;
        }
        catch (FormatException fe)
        {
            Console.WriteLine($"Decryption error: Input format is incorrect - {fe.Message}");
            return false;
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine($"Decryption error: Invalid argument - {ae.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decryption error: General exception - {ex.Message}");
            return false;
        }
    }
}
