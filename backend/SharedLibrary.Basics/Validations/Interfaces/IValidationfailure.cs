using System.Diagnostics.CodeAnalysis;

namespace QuVian.SharedLibrary.Validations.Interfaces;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public interface IValidationfailure
{
    /// <inheritdoc cref="IValidationfailure" />
    string propertyName { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    string errorMessage { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    object attemptedValue { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    object customState { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    int severity { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    string errorCode { get; set; }

    /// <inheritdoc cref="IValidationfailure" />
    IFormattedmessageplaceholdervalues formattedMessagePlaceholderValues { get; set; }
}