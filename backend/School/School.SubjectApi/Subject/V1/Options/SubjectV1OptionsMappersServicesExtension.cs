namespace School.SubjectApi.Subject.V1.Options;

// No mappers needed — the OPTIONS endpoint is synchronous with no CQRS dispatch.
[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1OptionsMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services) => services;
}
