using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.Endpoints.EndpointFilters.ExecutionTimes;

/// <summary>
///     Endpoint filter to add the execution time in the response header.
/// </summary>
public class ExecutionTimeEndpointFilter : IEndpointFilter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionTimeEndpointFilter" /> class.
    /// </summary>
    public ExecutionTimeEndpointFilter()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionTimeEndpointFilter" /> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Debug.Assert(next != null, nameof(next) + " != null");
        var invokeResult = await next.Invoke(context).ConfigureAwait(false);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        Debug.Assert(context != null, nameof(context) + " != null");
        context.HttpContext.Response.Headers["X-Execution-Time"] = elapsed.ToString(CultureInfo.InvariantCulture);
        return invokeResult;
    }
}
