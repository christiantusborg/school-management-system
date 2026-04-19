using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace Odin.Api.Base;

public static class CustomEndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpointsExcluding(
        this IEndpointRouteBuilder app,
        IEnumerable<Type> markerTypeSources,
        params Func<Type, bool>[] excludePredicates)
    {
        var markerInterface = typeof(IEndpointMarker);
        foreach (var markerType in markerTypeSources)
        {
            foreach (var type in markerType.Assembly.ExportedTypes)
            {
                if (!markerInterface.IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                    continue;
                if (excludePredicates.Any(pred => pred(type)))
                    continue;
                var instance = (IEndpointMarker)Activator.CreateInstance(type)!;
                instance.Map(app);
            }
        }
        return app;
    }
}
