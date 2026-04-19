using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;

/// <summary>
/// Contains extension methods for extracting raw data or results
/// from instances of the <see cref="SuccessOrFailure{TSuccess}" /> class.
/// </summary>
public static class SuccessOrFailureGetRawExtensions
{
    /// <summary>
    /// Attempts to retrieve the raw response from a <see cref="SuccessOrFailure{TSuccess}"/> instance if it represents a successful operation.
    /// If the operation is unsuccessful, returns the failure result.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="successOrFailure">The <see cref="SuccessOrFailure{TSuccess}"/> instance to retrieve the raw response from.</param>
    /// <param name="responseRaw">The raw response if the operation is successful; otherwise, <c>null</c>.</param>
    /// <param name="iResult">The failure result if the operation is unsuccessful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the operation is successful and a raw response is retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetResponseRaw<TResponse>(this SuccessOrFailure<TResponse> successOrFailure,
        out TResponse? responseRaw,
        out IResult? iResult)
    {
        if (!successOrFailure.IsSuccess)
        {
            iResult = successOrFailure.GetFaulted().ToResult();
            responseRaw = default;
            return false;
        }

        if (successOrFailure.success is null)
        {
            throw new ArgumentNullException(nameof(successOrFailure),
                "QuVian.SharedLibrary.Basics.SuccessOrFailures" + nameof(successOrFailure.success));
        }

        responseRaw = successOrFailure.success;
        iResult = null;
        return true;
    }
}