using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

[SuppressMessage("ReSharper", "SuggestVarOrType_BuiltInTypes")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
[SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
public static class SymmetricStringCryptographyExtensions
{
    public static bool SymmetricEncrypt<T>(this string plainText, Type  type   ,out string? encryptedText)
    {
        ArgumentNullException.ThrowIfNull(type);
        return SymmetricEncrypt(plainText, type.Name,out encryptedText);
    }
    public static bool SymmetricEncrypt(this string plainText, string key, out string? encryptedText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            encryptedText = null;
            return false;
        }

        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aesAlg.GenerateIV();
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    byte[] iv = aesAlg.IV;
                    byte[] encrypted = msEncrypt.ToArray();
                    byte[] combinedIvCt = new byte[iv.Length + encrypted.Length];
                    Array.Copy(iv, 0, combinedIvCt, 0, iv.Length);
                    Array.Copy(encrypted, 0, combinedIvCt, iv.Length, encrypted.Length);

                    encryptedText = Convert.ToBase64String(combinedIvCt);
                }
            }

            return true;
        }
        catch (Exception)
        {
            encryptedText = null;
            return false;
        }
    }

    public static bool SymmetricDecrypt(this string encryptedText, string key, out string? decryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            decryptedText = null;
            return false;
        }

        try
        {
            byte[] combinedIvCt = Convert.FromBase64String(encryptedText);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                byte[] iv = new byte[aesAlg.BlockSize / 8];
                byte[] cipherText = new byte[combinedIvCt.Length - iv.Length];

                Array.Copy(combinedIvCt, 0, iv, 0, iv.Length);
                Array.Copy(combinedIvCt, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            decryptedText = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return true;
        }
        catch
        {
            decryptedText = null;
            return false;
        }
    }
}