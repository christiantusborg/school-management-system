using System.Diagnostics.CodeAnalysis;

namespace QuVian.SharedLibrary.Validations.Interfaces;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
public interface IValidationfailures
{
    /// <inheritdoc cref="IValidationfailures" />
    string Command { get; set; }

    /// <inheritdoc cref="IValidationfailures" />
    Dictionary<string, string> CommandValues { get; set; }

    /// <inheritdoc cref="IValidationfailures" />
    string Origin { get; set; }

    /// <inheritdoc cref="IValidationfailures" />
    int HttpStatusCode { get; set; }

    /// <inheritdoc cref="IValidationfailures" />
    string Message { get; set; }

    /// <inheritdoc cref="IValidationfailures" />
    public IValidationfailure[]? ValidationFailures { get; set; }
}