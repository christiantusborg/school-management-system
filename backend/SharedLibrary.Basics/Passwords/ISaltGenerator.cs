namespace QuVian.SharedLibrary.Helpers.Cryptography.Passwords;

public interface ISaltGenerator
{
    byte[] Generate();
}