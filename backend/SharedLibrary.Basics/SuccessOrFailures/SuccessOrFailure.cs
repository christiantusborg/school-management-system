using System.Diagnostics.CodeAnalysis;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures;

/// <summary>
/// Represents a type that encapsulates the outcome of an operation, which can successOrFailure be a success value or a failure encapsulated in an exception.
/// </summary>
/// <typeparam name="TSuccess">The type of the value returned in case of success.</typeparam>
[SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
public class SuccessOrFailure<TSuccess>
{
    /// <summary>
    /// Indicates whether an operation has succeeded.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private",
        Justification = "We need use this reflection in Pipelines")]
    internal readonly bool IsSuccess = true;

    /// <summary>
    /// Represents the exception or fault that occurred during an operation.
    /// </summary>
    private readonly SuccessOrFailureException? faulted;

    /// <summary>
    /// Stores the result of an operation when it is successful.
    /// </summary>
    internal readonly TSuccess? success;

    /// <summary>
    /// Represents an SuccessOrFailure object that can hold a value of type <typeparamref name="TSuccess" /> or an error value.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    public SuccessOrFailure(SuccessOrFailureException successOrFailureException)
    {
        faulted = successOrFailureException;

        IsSuccess = false;
    }

    /// <summary>
    /// Represents a type that encapsulates the outcome of an operation, which can successOrFailure be a success value of type <typeparamref name="TSuccess" /> or a failure encapsulated in an exception.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the value returned in case of success.</typeparam>
    public SuccessOrFailure(TSuccess? success)
    {
        IsSuccess = true;
        this.success = success;
    }

    /// <summary>
    /// Retrieves the faulted exception contained within the instance.
    /// </summary>
    /// <returns>The faulted exception associated with the current instance.</returns>
    public SuccessOrFailureException GetFaulted()
    {
        return faulted!;
    }
}