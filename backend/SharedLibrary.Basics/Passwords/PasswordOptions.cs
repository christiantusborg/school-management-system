namespace QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

public class PasswordOptions
{
    public bool IncludeUppercase { get; init; } = true;
    public bool IncludeLowercase { get; init; } = true;
    public bool IncludeDigits { get; init; } = true;
    public bool IncludeSymbols { get; init; } = true;
    public int MinimumLength { get; init; } = 18;
    public int MinimumEntropy { get; init; } = 70;
    public bool IncludeUpperCaseSimilar { get; init; } = true;
    public bool IncludeLowerCaseSimilar { get; init; } = true;
    public bool IncludeDigitsSimilar { get; init; } = true;
}