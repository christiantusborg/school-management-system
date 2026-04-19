using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QuVian.SharedLibrary.Basics.Extensions;
using QuVian.SharedLibrary.Basics.Mappers;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;

namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="SuccessOrFailure{TSuccess}"/> instances,
/// allowing their transformation into <see cref="IResult"/> objects.
/// These methods support scenarios such as success or failure handling, resource creation, error mapping,
/// and dependency resolution initialization for streamlined application workflows.
/// </summary>
public static class SuccessOrFailureResultExtensions
{
    /// <summary>
    /// A static field that stores a reference to the service provider used for dependency injection
    /// to resolve services within the extension methods of the current class.
    /// </summary>
    private static IServiceProvider? s_serviceProvider; // Store a reference to the DI container.

    // Initialize the service provider (you may do this in your application startup).
    /// <summary>
    /// Initializes the service provider.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider to set, enabling dependency resolution for this extension class.
    /// </param>
    public static void Initialize(IServiceProvider? serviceProvider) => s_serviceProvider = serviceProvider;

    /// <summary>
    /// Converts a SuccessOrFailure object to an IResult object with the Created status code.
    /// </summary>
    /// <typeparam name="TInput">
    /// The type of the success value in the SuccessOrFailure object.
    /// </typeparam>
    /// <typeparam name="TOutput">
    /// The type of the output object.
    /// </typeparam>
    /// <param name="successOrFailure">
    /// The SuccessOrFailure object to be converted.
    /// </param>
    /// <param name="mapper">
    /// The mapper used to map the success value to the output object.
    /// </param>
    /// <param name="successOrFailureExceptionMapper">
    /// An optional mapper that maps exceptions within the SuccessOrFailure object to an IResult.
    /// </param>
    /// <returns>
    /// An IResult object representing the created resource.
    /// </returns>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
    public static IResult ToCreatedResult<TInput, TOutput>(this SuccessOrFailure<TInput> successOrFailure,
        IMapper<TInput, TOutput> mapper,
        IMapper<SuccessOrFailureException, IResult>? successOrFailureExceptionMapper = null)
    {
        var result = ToResult(successOrFailure, mapper, true, successOrFailureExceptionMapper);
        return result;
    }

    /// <summary>
    /// Converts a <see cref="SuccessOrFailure{TInput}"/> to an <see cref="IResult"/> using the provided mapper and optional configurations.
    /// </summary>
    /// <typeparam name="TInput">
    /// The type of the input data encapsulated by the <see cref="SuccessOrFailure{TInput}"/>.
    /// </typeparam>
    /// <typeparam name="TOutput">
    /// The type of the output data that will be mapped and included in the result.
    /// </typeparam>
    /// <param name="successOrFailure">
    /// The instance of <see cref="SuccessOrFailure{TInput}"/> containing the input data or error.
    /// </param>
    /// <param name="mapper">
    /// The mapper instance used to convert the input data of type <typeparamref name="TInput"/> to the output of type <typeparamref name="TOutput"/>.
    /// </param>
    /// <param name="isCreated">
    /// A boolean flag indicating whether the result is to be treated as a "Created" response. Defaults to false.
    /// </param>
    /// <param name="successOrFailureExceptionMapper">
    /// An optional mapper to convert a <see cref="SuccessOrFailureException"/> to an <see cref="IResult"/> in case of an error.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IResult"/> containing the mapped output or an error response, depending on the <see cref="SuccessOrFailure{TInput}"/> state.
    /// </returns>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
    public static IResult ToResult<TInput, TOutput>(this SuccessOrFailure<TInput> successOrFailure,
        IMapper<TInput, TOutput> mapper, bool isCreated = false,
        IMapper<SuccessOrFailureException, IResult>? successOrFailureExceptionMapper = null)
    {
        Debug.Assert(successOrFailure != null, nameof(successOrFailure) + " != null");
        Debug.Assert(mapper != null, nameof(mapper) + " != null");

        Debug.Assert(s_serviceProvider != null, nameof(s_serviceProvider) + " != null");

        var httpContextAccessor = s_serviceProvider.GetService<IHttpContextAccessor>();

        Debug.Assert(httpContextAccessor != null, nameof(httpContextAccessor) + " != null");
        Debug.Assert(httpContextAccessor.HttpContext != null);

        if (!successOrFailure.IsSuccess)
        {
            return successOrFailure.GetFaulted().ToResultMapped(successOrFailureExceptionMapper);
        }

        if (successOrFailure.success is null)
        {
            throw new ArgumentNullException(nameof(successOrFailure),
                "QuVian.SharedLibrary.Basics.SuccessOrFailures" + nameof(successOrFailure.success));
        }

        var properties = typeof(TOutput).GetProperties();

        if (isCreated && properties.Length <= 0)
        {
            return Results.Accepted();
        }
        else if (!isCreated && properties.Length <= 0)
        {
            var responseObject = Activator.CreateInstance(typeof(TOutput));

            return Results.Ok(responseObject);
        }

        var success = (TInput)Convert.ChangeType(successOrFailure.success, typeof(TInput), CultureInfo.InvariantCulture);

        var response = mapper.MapFrom(success);
        if (!isCreated)
            return Results.Ok(response);

        var currentUrl = httpContextAccessor.HttpContext.Request.Path.Value;
        Debug.Assert(currentUrl != null, nameof(currentUrl) + " != null");

        var keyProperty = typeof(TOutput).GetPropertyWithAttribute<KeyAttribute>();

        if (keyProperty != null)
        {
            var idValue = keyProperty.GetValue(response);
            currentUrl += "/" + idValue;
        }

        var createdUri = currentUrl;
        return Results.Created(createdUri, response);
    }
}