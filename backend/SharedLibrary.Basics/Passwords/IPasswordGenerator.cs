namespace QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

public interface IPasswordGenerator
{
    string Generate(int length = 27);
    string Generate(PasswordOptions options, int length = 27);
}