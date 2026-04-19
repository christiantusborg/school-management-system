namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;

public interface IAsymmetricStringCryptography
{
    public Task<string> AsymmetricEncryptAsync(string plainText, Type publicKey);
    public Task<string> AsymmetricEncryptAsync(string plainText, string publicKey);

    public Task<string> AsymmetricDecryptAsync(string encryptedText, Type privateKey);
    public Task<string> AsymmetricDecryptAsync(string encryptedText, string privateKey);
}