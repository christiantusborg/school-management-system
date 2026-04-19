using System.Security.Cryptography;
using System.Text;

namespace QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

public class PasswordGenerator : IPasswordGenerator
{
    private readonly RandomNumberGenerator _rng;
    private const string UpperCase = "ABCDEFGHKMNPQRSTUVWXYZ";
    private const string UpperCaseSimilar = "IJLO";
    private const string LowerCase = "abcdefghkmnpqrstuvwxyz";
    private const string LowerCaseSimilar = "ijlo";
    private const string Digits = "23456789";
    private const string DigitsSimilar = "01";
    private const string Symbols = "!@$%^&*-_=+";

    public PasswordGenerator(RandomNumberGenerator rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public string Generate(int length = 27)
    {
        PasswordOptions options = new PasswordOptions
        {
            IncludeUppercase = true,
            IncludeUpperCaseSimilar = false,
            IncludeLowercase = true,
            IncludeLowerCaseSimilar = false,
            IncludeDigits = true,
            IncludeDigitsSimilar = false,
            IncludeSymbols = true,
            MinimumLength = 18,
            MinimumEntropy = 70,
        };

        return Generate(options, length);
    }

    public string Generate(PasswordOptions options, int length = 27)
    {

        if (length < options.MinimumLength)
            throw new ArgumentOutOfRangeException("Password_length_must_be_at_least_" + options.MinimumLength + "_characters");

        string password;
        do {
            password = GenerateSecurePassword(options, length);
        } while (CalculateEntropy(password) < options.MinimumEntropy || !EnsureMinimumPresence(password, options));  // Ensure sufficient entropy or any other threshold

        return password;
    }

    private string GenerateSecurePassword(PasswordOptions options, int length)
    {
        StringBuilder password = new StringBuilder();
        List<char> availableCharacters = new List<char>();

        if (options.IncludeUppercase) availableCharacters.AddRange(UpperCase);
        if (options.IncludeUpperCaseSimilar) availableCharacters.AddRange(UpperCaseSimilar);
        if (options.IncludeLowercase) availableCharacters.AddRange(LowerCase);
        if (options.IncludeLowerCaseSimilar) availableCharacters.AddRange(LowerCaseSimilar);
        if (options.IncludeDigits) availableCharacters.AddRange(Digits);
        if (options.IncludeDigitsSimilar) availableCharacters.AddRange(DigitsSimilar);
        if (options.IncludeSymbols) availableCharacters.AddRange(Symbols);

        while (password.Length < length)
        {
            char nextChar = GetRandomCharacter(availableCharacters.ToArray());
            password.Append(nextChar);
        }

        return password.ToString();
    }

    public bool EnsureMinimumPresence(string password, PasswordOptions options)
    {
        // Ensure at least one of each selected character type

        if (options.IncludeUppercase && !password.Any(char.IsUpper))
            return false;

        if (options.IncludeLowercase && !password.Any(char.IsLower))
            return false;

        if (options.IncludeDigits && !password.Any(char.IsDigit))
            return false;

        if (options.IncludeSymbols && !password.Any(c => Symbols.Contains(c)))
            return false;

        return true;
    }

    public char GetRandomCharacter(char[] characters)
    {
        var randomNumber = new byte[1];
        _rng.GetBytes(randomNumber);
        int value = randomNumber[0] % characters.Length;
        return characters[value];
    }
    public double CalculateEntropy(string password)
    {
        var poolSize = 0;
        bool hasUpperCase = password.Any(char.IsUpper);
        bool hasLowerCase = password.Any(char.IsLower);
        bool hasDigits = password.Any(char.IsDigit);
        bool hasSymbols = password.Any(c => Symbols.Contains(c));

        // Determine the character pool size adjustments based on the exclusive presence of each character type.
        if (hasUpperCase)
            poolSize += (hasLowerCase || hasDigits || hasSymbols) ? UpperCase.Length : UpperCase.Length / 2;
        if (hasLowerCase)
            poolSize += (hasUpperCase || hasDigits || hasSymbols) ? LowerCase.Length : LowerCase.Length / 2;
        if (hasDigits)
            poolSize += (hasUpperCase || hasLowerCase || hasSymbols) ? Digits.Length : Digits.Length / 2;
        if (hasSymbols)
            poolSize += (hasUpperCase || hasLowerCase || hasDigits) ? Symbols.Length : Symbols.Length / 2;

        return password.Length * Math.Log2(poolSize);
    }

}
