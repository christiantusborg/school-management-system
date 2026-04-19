using System.Globalization;

namespace QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

/// <summary>
/// Provides extension methods for capitalizing strings.
/// </summary>
public static class StringExtensionCapitalize
{
    /// <summary>
    /// Capitalizes the first character of a string.
    /// </summary>
    /// <param name="input">The string to capitalize.</param>
    /// <returns>The capitalized string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static string Capitalize(this string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input), "Input string cannot be null.");
        }

        if (input.Length == 0)
        {
            return string.Empty;
        }

        return string.Create(input.Length, input, (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            span[0] = char.ToUpper(span[0], CultureInfo.InvariantCulture);
        });
    }
}