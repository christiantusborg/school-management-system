using Microsoft.AspNetCore.Routing;

namespace QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

/// <summary>
///     An interface that marks a class as an endpoint for routing purposes in an ASP.NET Core application.
/// </summary>
public interface IEndpointMarker
{
    /// <summary>
    ///     Maps one or more endpoints to the specified <paramref name="app" /> builder and returns it.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder" /> builder to map endpoints to.</param>
    /// <returns>The same <paramref name="app" /> builder after endpoints have been added.</returns>
    IEndpointRouteBuilder Map(IEndpointRouteBuilder app);
}
