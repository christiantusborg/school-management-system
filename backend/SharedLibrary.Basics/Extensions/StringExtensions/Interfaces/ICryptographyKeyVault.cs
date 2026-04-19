using System.Security.Cryptography;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;

public interface ICryptographyKeyVault
{
    Task CreateAsymmetricAsync(string publicKey);
    Task CreateSymmetricAsync(string publicKey);
    Task<RSA> LoadAsymmetricAsync(string keyName, KeyType keyType);
}