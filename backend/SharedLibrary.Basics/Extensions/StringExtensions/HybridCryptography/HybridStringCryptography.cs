using System.Text;
using QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;
using QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.HybridCryptography;

public class HybridStringCryptography : IHybridStringCryptography
{
    private readonly IAsymmetricStringCryptography _asymmetricStringCryptography;
    private readonly ISymmetricStringCryptography _symmetricStringCryptography;
    private readonly IPasswordGenerator _passwordGenerator;

    public HybridStringCryptography(IAsymmetricStringCryptography asymmetricStringCryptography,ISymmetricStringCryptography symmetricStringCryptography, IPasswordGenerator passwordGenerator)
    {
        _asymmetricStringCryptography = asymmetricStringCryptography;
        _symmetricStringCryptography = symmetricStringCryptography;
        _passwordGenerator = passwordGenerator;
    }
    public async Task<string> HybridEncryptAsync(string plainText, Type keyTypeName)
    {
        return await HybridEncryptAsync(plainText, keyTypeName.Name);
    }

    public async Task<string> HybridEncryptAsync(string plainText, string publicKey)
    {
        var symmetricKey = _passwordGenerator.Generate();
        var textEncrypted = _symmetricStringCryptography.SymmetricEncryptWithPassword(plainText, symmetricKey);
        var symmetricKeyEncrypted = await _asymmetricStringCryptography.AsymmetricEncryptAsync(symmetricKey, publicKey);

        StringBuilder sb = new StringBuilder();
        sb.Append(textEncrypted);
        sb.Append("#");
        sb.Append(symmetricKeyEncrypted);

        var result = sb.ToString();
        return result;

    }

    public async Task<string> HybridDecryptAsync(string encryptedText, Type keyTypeName)
    {
        return await HybridDecryptAsync(encryptedText, keyTypeName.Name);
    }

    public async Task<string> HybridDecryptAsync(string encryptedText, string privateKey)
    {
        var  encryptedTextSplit = encryptedText.Split("#");
        var symmetricKey = encryptedTextSplit.Last();
        var textEncrypted = encryptedTextSplit.First();

        var symmetricKeyDecrypted = await _asymmetricStringCryptography.AsymmetricDecryptAsync(symmetricKey, privateKey);
        var textDecrypted =
            _symmetricStringCryptography.SymmetricDecryptWithPassword(textEncrypted, symmetricKeyDecrypted);

        return textDecrypted;
    }
}