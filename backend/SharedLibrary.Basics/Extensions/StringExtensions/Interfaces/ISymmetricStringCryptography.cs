using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;

public interface ISymmetricStringCryptography
{
    public string SymmetricEncrypt(string plainText, Type publicKey);
    public string SymmetricEncrypt(string plainText, string publicKey);
    public string SymmetricEncryptWithPassword(string plainText, string password);

    public string SymmetricDecrypt(string encryptedText, Type privateKey);
    public string SymmetricDecrypt(string encryptedText, string privateKey);
    public string SymmetricDecryptWithPassword(string encryptedText, string password);
}

public class SymmetricStringCryptography : ISymmetricStringCryptography
{
    public string SymmetricEncrypt(string plainText, Type publicKey)
    {
        throw new NotImplementedException();
    }

    public string SymmetricEncrypt(string plainText, string publicKey)
    {
        throw new NotImplementedException();
    }

    public string SymmetricEncryptWithPassword(string plainText, string password)
    {
        var encryptedText = String.Empty;
        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(password.PadRight(32).Substring(0, 32));
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

            return encryptedText;
        }
        catch
        {
            //TODO We need to add log here
            throw;
        }
    }

    public string SymmetricDecrypt(string encryptedText, Type privateKey)
    {
        throw new NotImplementedException();
    }

    public string SymmetricDecrypt(string encryptedText, string privateKey)
    {
        throw new NotImplementedException();
    }

    public string SymmetricDecryptWithPassword(string encryptedText, string password)
    {

        var decryptedText = String.Empty;
        try
        {
            byte[] combinedIvCt = Convert.FromBase64String(encryptedText);
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(password.PadRight(32).Substring(0, 32));
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

            return decryptedText;
        }
        catch
        {
            //TODO We Need to add logs
            throw;
        }
    }
}