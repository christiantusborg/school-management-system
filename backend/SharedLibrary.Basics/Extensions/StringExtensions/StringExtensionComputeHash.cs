using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

public static class StringExtensionComputeHash
{
    public static string ComputeHash(this string rawData)
    {
        // Create a SHA256
        using SHA256 sha256Hash = SHA256.Create();
        // ComputeHash - returns byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

        // Convert byte array to a string
        var builder = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}