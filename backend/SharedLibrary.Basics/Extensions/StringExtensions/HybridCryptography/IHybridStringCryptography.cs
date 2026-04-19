namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.HybridCryptography;

public interface IHybridStringCryptography
{
    public Task<string> HybridEncryptAsync(string plainText, Type publicKey);
    public Task<string> HybridEncryptAsync(string plainText, string publicKey);

    public Task<string> HybridDecryptAsync(string encryptedText, Type privateKey);
    public Task<string> HybridDecryptAsync(string encryptedText, string privateKey);
}