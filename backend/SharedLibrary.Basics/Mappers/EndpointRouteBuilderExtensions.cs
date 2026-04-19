using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace QuVian.SharedLibrary.Basics.Mappers;

/// <summary>
///     Provides extension methods to register endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Maps all endpoints that implement the <see cref="IMapperMarker" /> interface from the specified types.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IEndpointRouteBuilder" /> instance to map endpoints to.</param>
    /// <param name="types">The list of <see cref="Type" /> instances to scan for endpoint implementations.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder" /> instance.</returns>
    public static IServiceCollection AddMappers(this IServiceCollection serviceProvider, params Type[] types)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());

        Trace.WriteLine("Starting AddMappers...");
        Debug.Assert(types != null, nameof(types) + " != null");

        var markerType = typeof(IMapperMarker);

        foreach (var type in types)
        {
            Trace.WriteLine($"Scanning assembly: {type.Assembly.FullName}");

            var assembly = type.Assembly;

            foreach (var exportedType in assembly.ExportedTypes.Where(x =>
                         markerType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract))
            {
                Trace.WriteLine($"Found mapper: {exportedType.FullName}");

                try
                {
                    var endpointMarkerInstance = (IMapperMarker)Activator.CreateInstance(exportedType)!;
                    endpointMarkerInstance.Map(serviceProvider);
                    Trace.WriteLine($"Successfully mapped: {exportedType.FullName}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error creating or mapping {exportedType.FullName}: {ex.Message}");
                }
            }
        }

        Trace.WriteLine("Completed AddMappers.");
        return serviceProvider;
    }
}
