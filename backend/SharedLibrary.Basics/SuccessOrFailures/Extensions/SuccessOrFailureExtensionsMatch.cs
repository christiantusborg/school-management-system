using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;

/// <summary>
/// Contains extension methods that facilitate matching operations for objects of type SuccessOrFailure.
/// </summary>
public static class SuccessOrFailureExtensionsMatch
{
    /// <summary>
    /// Matches the SuccessOrFailure object with the provided functions.
    /// </summary>
    /// <typeparam name="T">The return type of the functions.</typeparam>
    /// <typeparam name="TSuccess">The type of the success value in the SuccessOrFailure object.</typeparam>
    /// <param name="successOrFailure">The SuccessOrFailure object to match against.</param>
    /// <param name="faulted">The function to execute if the SuccessOrFailure object is in a faulted state.</param>
    /// <param name="success">The function to execute if the SuccessOrFailure object contains a success value.</param>
    /// <returns>The result of executing the function corresponding to the state of the SuccessOrFailure object.</returns>
    public static T Match<T, TSuccess>(this SuccessOrFailure<TSuccess> successOrFailure,
        Func<SuccessOrFailureException, T> faulted, Func<TSuccess, T> success)
    {
        ArgumentNullException.ThrowIfNull(successOrFailure);

        return successOrFailure.IsSuccess ? success(successOrFailure.success!) : faulted(successOrFailure.GetFaulted());
    }
}