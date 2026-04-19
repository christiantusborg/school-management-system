using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides extension methods for configuring endpoints in an ASP.NET Core application.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps endpoints defined in the specified types to the provided endpoint route builder.
    /// Only types implementing <see cref="IEndpointMarker"/> and meeting required criteria
    /// (non-interface, non-abstract) are processed.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> instance to map the endpoints to.</param>
    /// <param name="types">An array of types whose assemblies will be scanned for endpoint definitions.</param>
    /// <returns>The modified <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, params Type[] types)
    {
        Debug.Assert(types != null, nameof(types) + " != null");
        var markerType = typeof(IEndpointMarker);

        foreach (var type in types)
        {
            var assembly = type.Assembly;
            foreach (var exportedType in assembly.ExportedTypes.Where(x =>
                         markerType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
            {
                var endpointMarkerInstance = (IEndpointMarker)Activator.CreateInstance(exportedType)!;
                endpointMarkerInstance.Map(app);
            }
        }

        // Return the endpoint route builder.
        return app;
    }
}

