using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.Endpoints.EndpointFilters.TraceIdentifierFilters;

public class TraceIdentifierFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var traceId = context.HttpContext.TraceIdentifier; // Get the trace ID

        var result = await next(context); // Proceed with the request

        // Add TraceIdentifier to the response headers
        context.HttpContext.Response.Headers["X-Trace-Identifier"] = traceId;

        return result;
    }
}
