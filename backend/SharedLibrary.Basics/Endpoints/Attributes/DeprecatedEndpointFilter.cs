using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;

/// <summary>
///     Represents a filter that checks for deprecated endpoints and modifies the response accordingly.
/// </summary>
/// <remarks>
///     This filter looks for the <see cref="DeprecatedAttribute" /> on endpoints. If an endpoint
///     is marked as deprecated, it adds headers indicating the deprecation and end-of-life dates.
///     If the current date is past the end-of-life date, it throws an exception, preventing the execution of the endpoint.
/// </remarks>
public class DeprecatedEndpointFilter : IEndpointFilter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DeprecatedEndpointFilter" /> class.
    /// </summary>
    public DeprecatedEndpointFilter()
    {
    }

    /// <summary>
    ///     Invokes the filter logic asynchronously.
    /// </summary>
    /// <param name="context">The <see cref="EndpointFilterInvocationContext" /> providing context for the filter execution.</param>
    /// <param name="next">The delegate representing the remaining filter pipeline.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the endpoint is accessed past its end-of-life date.</exception>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        Debug.Assert(context != null, nameof(context) + " != null");
        var endpoint = context.HttpContext.GetEndpoint();

        var declaredType = endpoint?.Metadata
            .OfType<MethodInfo>()
            .FirstOrDefault();

        var deprecatedAttr = declaredType?.DeclaringType?
            .GetCustomAttributes(typeof(DeprecatedAttribute), false)
            .Cast<DeprecatedAttribute>()
            .FirstOrDefault();

        context.HttpContext.Response.Headers.Append("x-Deprecated", "n/a");
        context.HttpContext.Response.Headers.Append("x-EndOfLife", "n/a");

        if (deprecatedAttr != null)
        {
            context.HttpContext.Response.Headers.Append("x-Deprecated", deprecatedAttr.DeprecatedDate.ToString(CultureInfo.InvariantCulture));
            context.HttpContext.Response.Headers.Append("x-EndOfLife", deprecatedAttr.EndOfLifeDate.ToString(CultureInfo.InvariantCulture));

            if (deprecatedAttr.EndOfLifeDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return Results.Problem("This_endpoint_has_been_deprecated_and_is_no_longer_available",
                    null,
                    StatusCodes.Status410Gone);
            }
        }

        Debug.Assert(next != null, nameof(next) + " != null");
        return await next(context).ConfigureAwait(false);
    }
}

