using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;

/// <summary>
/// Provides helper methods for creating <see cref="SuccessOrFailure{T}" /> instances with pre-defined error messages
/// and HTTP status codes for various scenarios involving entities.
/// </summary>
/// <typeparam name="T">The type of the success value in the <see cref="SuccessOrFailure{T}" /> instance.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
[SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code")]
public static class SuccessOrFailureHelper<T>
{
    /// <summary>
    /// Creates an <see cref="entity" /> instance representing an entity not found error with a pre-defined error
    /// message and HTTP status code.
    /// </summary>
    /// <param name="entity">The type of the entity that was not found.</param>
    /// <returns>An <see cref="SuccessOrFailure{TSuccess}" /> instance representing an entity not found error.</returns>
    public static SuccessOrFailure<T> EntityNotFound(Type entity)
    {
        Debug.Assert(entity != null, new StringBuilder().Append(nameof(entity)).Append(" != null").ToString());
        var stringBuilder = new StringBuilder().Append("Entity_").Append(entity.Name).Append("_NotFound").ToString();
        return Create(stringBuilder, StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Creates an <see cref="entity" /> instance representing an entity already exists error with a pre-defined error
    /// message and HTTP status code.
    /// </summary>
    /// <param name="entity">The type of the entity that already exists.</param>
    /// <returns>An <see cref="SuccessOrFailure{TSuccess}" /> instance representing an entity already exists error.</returns>
    public static SuccessOrFailure<T> EntityAlreadyExists(Type entity)
    {
        Debug.Assert(entity != null, new StringBuilder().Append(nameof(entity)).Append(" != null").ToString());
        var stringBuilder = new StringBuilder().Append("Entity_").Append(entity.Name).Append("_AlreadyExists")
            .ToString();
        return Create(stringBuilder, StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Creates an <see cref="entity" /> instance representing an administrator role required error with a pre-defined
    /// error message and HTTP status code.
    /// </summary>
    /// <param name="entity">The type of the entity for which the administrator role is required.</param>
    /// <returns>An <see cref="SuccessOrFailure{TSuccess}" /> instance representing an administrator role required error.</returns>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static SuccessOrFailure<T> AdministratorRoleRequired(Type entity)
    {
        Debug.Assert(entity != null, new StringBuilder().Append(nameof(entity)).Append(" != null").ToString());
        var stringBuilder = new StringBuilder().Append("AdministratorRoleRequired_").Append(entity.Name).ToString();
        return Create(stringBuilder, StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Creates an <see cref="entity" /> instance representing a bad permissions error with a pre-defined error message
    /// and HTTP status code.
    /// </summary>
    /// <param name="entity">The type of the entity for which the permissions are insufficient or invalid.</param>
    /// <returns>An <see cref="SuccessOrFailure{TSuccess}" /> instance representing a bad permissions error.</returns>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static SuccessOrFailure<T> BadPermissions(Type entity)
    {
        Debug.Assert(entity != null, new StringBuilder().Append(nameof(entity)).Append(" != null").ToString());
        var stringBuilder = new StringBuilder().Append("BadPermissions_").Append(entity.Name).ToString();
        return Create(stringBuilder, StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Creates an instance of <see cref="SuccessOrFailure{TSuccess}" /> representing an error with the provided error message and HTTP status code.
    /// </summary>
    /// <param name="message">The error message describing the issue.</param>
    /// <param name="statusCodes">The HTTP status code associated with the error. Defaults to <see cref="StatusCodes.Status400BadRequest" />.</param>
    /// <returns>An instance of <see cref="SuccessOrFailure{TSuccess}" /> encapsulating the specified error and status code.</returns>
    public static SuccessOrFailure<T> Create(string message, int statusCodes = StatusCodes.Status400BadRequest)
    {
        var stackTraceFrame = new StackTrace().GetFrame(2)?.GetMethod()?.DeclaringType?.DeclaringType?.FullName;
        var callingMethod = stackTraceFrame ?? "Unknown";

        var successOrFailureError = new SuccessOrFailureException(message, callingMethod, statusCodes);
        var successOrFailure = new SuccessOrFailure<T>(successOrFailureError);

        return successOrFailure;
    }
}

