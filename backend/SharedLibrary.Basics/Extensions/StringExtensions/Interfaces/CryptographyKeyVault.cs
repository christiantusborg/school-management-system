using System.Security.Cryptography;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions.Interfaces;

public class CryptographyKeyVault : ICryptographyKeyVault
{
    public Task CreateAsymmetricAsync(string keyName)
    {
        using (RSA rsa = RSA.Create(2048)) // Use 2048-bit RSA keys
        {
            // File paths for saving the keys
            string privateKeyPath = keyName + ".pri";
            string publicKeyPath = keyName + ".pub";

            // Save the keys to files

            File.WriteAllText(privateKeyPath, Convert.ToBase64String(rsa.ExportRSAPrivateKey()));
            File.WriteAllText(publicKeyPath, Convert.ToBase64String(rsa.ExportRSAPublicKey()));
            Console.WriteLine("Keys saved successfully.");
        }

        return Task.CompletedTask;
    }

    public Task CreateSymmetricAsync(string publicKey)
    {
        throw new NotImplementedException();
    }

    public async Task<RSA> LoadAsymmetricAsync(string keyName, KeyType keyType)
    {
        var rsa = RSA.Create();

        if (!File.Exists(keyName + ".pub"))
            await CreateAsymmetricAsync(keyName).ConfigureAwait(false);

        switch (keyType)
        {
            case KeyType.AsymmetricPrivate:
                var privateBase64 = await File.ReadAllTextAsync(keyName + ".pri");
                var privateBytes = Convert.FromBase64String(privateBase64);
                rsa.ImportRSAPrivateKey(privateBytes, out _);
                Console.WriteLine("Private key loaded successfully.");
                break;
            case KeyType.AsymmetricPublic:
                var publicBase64 = await File.ReadAllTextAsync(keyName + ".pub");
                var publicBytes = Convert.FromBase64String(publicBase64);
                rsa.ImportRSAPublicKey(publicBytes, out _);
                Console.WriteLine("Public key loaded successfully.");
                break;
            case KeyType.Symmetric:
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }

        return rsa;
    }

    public async Task<string> LoadSymmetricAsync(string keyName, KeyType keyType)
    {
        var symmetric = String.Empty;

        switch (keyType)
        {
            case KeyType.Symmetric:
                symmetric = await File.ReadAllTextAsync(keyName + "sym");
                Console.WriteLine("Private key loaded successfully.");
                break;
            case KeyType.AsymmetricPublic:
            case KeyType.AsymmetricPrivate:
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }

        return symmetric;
    }
}