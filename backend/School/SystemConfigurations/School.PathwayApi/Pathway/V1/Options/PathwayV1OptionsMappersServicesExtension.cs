namespace School.PathwayApi.Pathway.V1.Options;

// No mappers needed — the OPTIONS endpoint is synchronous with no CQRS dispatch.
[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1OptionsMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services) => services;
}
