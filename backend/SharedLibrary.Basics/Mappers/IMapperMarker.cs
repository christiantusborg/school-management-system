namespace QuVian.SharedLibrary.Basics.Mappers;

/// <summary>
///     Represents an interface for marker classes that provide mapping functionality.
/// </summary>
public interface IMapperMarker
{
    /// <summary>
    ///     Maps the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The mapped service collection.</returns>
    IServiceCollection Map(IServiceCollection services);
}
