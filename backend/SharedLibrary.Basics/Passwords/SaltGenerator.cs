using System.Security.Cryptography;

namespace QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

public class SaltGenerator : ISaltGenerator
{
    public byte[] Generate()
    {
        // Implement a method to generate a secure, random salt
        byte[] salt = new byte[16];
        using (var random = new RNGCryptoServiceProvider())
        {
            random.GetBytes(salt);
        }
        return salt;
    }
}