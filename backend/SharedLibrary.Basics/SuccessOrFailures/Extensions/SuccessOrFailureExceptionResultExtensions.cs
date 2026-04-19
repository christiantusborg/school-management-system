using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuVian.SharedLibrary.Basics.Mappers;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Exceptions;


namespace QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;

/// <summary>
/// Provides extension methods for handling <see cref="SuccessOrFailureException" /> instances
/// to create and manage <see cref="IResult" /> objects, with optional mapping support.
/// </summary>
public static class SuccessOrFailureExceptionResultExtensions
{
    /// <summary>
    /// Converts the specified <see cref="SuccessOrFailureException" /> instance to an <see cref="IResult" /> object.
    /// </summary>
    /// <param name="successOrFailureException">
    /// The <see cref="SuccessOrFailureException" /> instance to convert.
    /// </param>
    /// <returns>
    /// An <see cref="IResult" /> object representing the data derived from the <paramref name="successOrFailureException" />.
    /// </returns>
    public static IResult ToResult(this SuccessOrFailureException successOrFailureException) => successOrFailureException.ToResultMapped();

    /// <summary>
    /// Converts the specified <see cref="SuccessOrFailureException" /> instance to an <see cref="IResult" /> object,
    /// optionally using a provided mapper to customize the conversion.
    /// </summary>
    /// <param name="successOrFailureException">
    /// The <see cref="SuccessOrFailureException" /> instance to convert.
    /// </param>
    /// <param name="successOrFailureExceptionMapper">
    /// An optional mapper to transform the <see cref="SuccessOrFailureException" /> into an <see cref="IResult" /> object.
    /// If no mapper is provided, a default conversion will be performed.
    /// </param>
    /// <returns>
    /// An <see cref="IResult" /> object representing the converted data or default formatted result
    /// based on the input <paramref name="successOrFailureException" />.
    /// </returns>
    public static IResult ToResultMapped(this SuccessOrFailureException successOrFailureException,
        IMapper<SuccessOrFailureException, IResult>? successOrFailureExceptionMapper = null)
    {
        Debug.Assert(successOrFailureException != null, nameof(successOrFailureException) + " != null");
        var httpStatusCode = GetHttpStatusCode(successOrFailureException);
        var exceptionResult = GetExceptionResult(successOrFailureException);

        if (successOrFailureExceptionMapper != null)
        {
            return successOrFailureExceptionMapper.MapFrom(successOrFailureException);
        }

        try
        {
            var result = Results.Json(exceptionResult,new JsonSerializerOptions(),null,httpStatusCode);
            //var result = Results.Json(exceptionResult, exceptionResult.GetType(), jsonSerializerContext , null, httpStatusCode);
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Gets the HTTP status code associated with the specified <see cref="Exception" /> instance.
    /// </summary>
    /// <param name="exception">
    /// The <see cref="Exception" /> instance to retrieve the status code for.
    /// </param>
    /// <returns>
    /// The HTTP status code associated with the <paramref name="exception" />, or 500 if no status code is found.
    /// </returns>
    private static int GetHttpStatusCode(Exception exception)
    {
        var httpStatusCode = 500;
        if (exception.Data.Contains("HttpStatusCode"))
        {
            httpStatusCode = (int)(exception.Data["HttpStatusCode"] ?? 500);
        }

        return httpStatusCode;
    }

    /// <summary>
    /// Retrieves the result associated with the provided <see cref="Exception" /> instance.
    /// </summary>
    /// <param name="exception">
    /// The <see cref="Exception" /> instance from which the result is extracted.
    /// </param>
    /// <returns>
    /// An object representing the result contained in the <paramref name="exception" />,
    /// or an instance of <see cref="SuccessOrFailureNothing" /> if no result is found.
    /// </returns>
    private static object GetExceptionResult(Exception exception)
    {
        return (exception.Data.Contains("Result") ? exception.Data["Result"] : new SuccessOrFailureNothing())!;
    }
}