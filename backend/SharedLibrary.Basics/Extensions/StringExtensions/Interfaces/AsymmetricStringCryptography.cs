using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;

public class AsymmetricStringCryptography : IAsymmetricStringCryptography

{
    private readonly ICryptographyKeyVault _cryptographyKeyVault;

    public AsymmetricStringCryptography(ICryptographyKeyVault cryptographyKeyVault)
    {
        _cryptographyKeyVault = cryptographyKeyVault;
    }
    public async Task<string> AsymmetricEncryptAsync(string plainText, Type publicKey)
    {
        return await AsymmetricEncryptAsync(plainText, publicKey.Name);
    }

    public async Task<string> AsymmetricEncryptAsync(string plainText, string keyName)
    {
        var publicKey = await _cryptographyKeyVault.LoadAsymmetricAsync(keyName, KeyType.AsymmetricPublic);
        var data = Encoding.UTF8.GetBytes(plainText);

        var encryptedData = publicKey.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        return Convert.ToBase64String(encryptedData);
    }

    public async Task<string> AsymmetricDecryptAsync(string encryptedText, Type privateKey)
    {
        return await AsymmetricDecryptAsync(encryptedText, privateKey.Name);
    }

    public async Task<string> AsymmetricDecryptAsync(string encryptedText, string privateKey)
    {
        var privateKeyRsa = await _cryptographyKeyVault.LoadAsymmetricAsync(privateKey,KeyType.AsymmetricPrivate);
        var encryptedTextData = Convert.FromBase64String(encryptedText);
        var decryptedData = privateKeyRsa.Decrypt(encryptedTextData, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decryptedData);
    }







}