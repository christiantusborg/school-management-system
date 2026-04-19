using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;
using QuVian.SharedLibrary.Permissions;
using QuVian.SharedLibrary.Validations.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides extension methods for adding various HTTP verb-based route handler endpoints
/// to an <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class MapGetRouteHandlerBuilderExtensions
{
    /// Maps a POST endpoint with the specified handler, command, and response types, using a dynamic endpoint name
    /// associated with the given endpoint marker. Adds default API documentation and configuration settings to
    /// the endpoint, such as response metadata.
    /// <typeparam name="TCommand">The type of the command object expected by the handler.</typeparam>
    /// <typeparam name="TResponse">The type of the response object returned by the handler.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to which the endpoint will be added.</param>
    /// <param name="endpoint">The marker object that identifies the associated endpoint.</param>
    /// <param name="handler">The delegate to handle incoming HTTP requests to this POST endpoint.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> instance for further configuration of the endpoint.</returns>
    public static RouteHandlerBuilder MapPost<TCommand, TResponse>(this IEndpointRouteBuilder endpoints, IEndpointMarker endpoint, Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();

        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri, handler);
        var result = endpoints.MapPost(endpointName, handler)
            .AddDefaultProduces<TResponse, TCommand>(classType,handler)
            .Produces<TResponse>(StatusCodes.Status201Created);
        return result;
    }

    /// Maps a DELETE HTTP endpoint for the specified command and response types and adds default configurations to the route handler builder.
    /// <typeparam name="TCommand">The type of the command object associated with the DELETE request.</typeparam>
    /// <typeparam name="TResponse">The type of the response object associated with the DELETE request.</typeparam>
    /// <param name="endpoints">The endpoint route builder used to configure the route handler.</param>
    /// <param name="endpoint">A marker interface that defines metadata or characteristics of the endpoint.</param>
    /// <param name="handler">The delegate that handles the DELETE request.</param>
    /// <returns>A configured RouteHandlerBuilder.</returns>
    public static RouteHandlerBuilder MapDelete<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        IEndpointMarker endpoint,
        Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();

        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri,handler);
        var result = endpoints.MapDelete(endpointName, handler)
            .AddDefaultProduces<TResponse, TCommand>(classType,handler)
            .Produces<TResponse>();
        return result;
    }

    /// Maps a GET HTTP endpoint to a specified route for handling requests with provided command and response types.
    /// <param name="endpoints">
    /// The endpoint route builder to which the GET route will be added.
    /// </param>
    /// <param name="endpoint">
    /// The endpoint marker containing route specifics used to define the route.
    /// </param>
    /// <param name="handler">
    /// The delegate representing the handler logic for the GET route.
    /// </param>
    /// <typeparam name="TCommand">
    /// The type of the command parameter expected by the route handler.
    /// </typeparam>
    /// <typeparam name="TResponse">
    /// The type of the response object returned by the route handler.
    /// </typeparam>
    /// <returns>
    /// A RouteHandlerBuilder object representing the configured route with additional metadata for producing responses.
    /// </returns
    public static RouteHandlerBuilder MapGet<TCommand, TResponse>(this IEndpointRouteBuilder endpoints, IEndpointMarker endpoint, Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();

        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri,handler);
        var result = endpoints.MapGet(endpointName, handler)
            .AddDefaultProduces<TResponse, TCommand>(classType,handler)
            .Produces<TResponse>();
        return result;
    }


    /// Maps a PATCH HTTP method to the specified endpoint and handler, and adds default response type produces.
    /// <typeparam name="TCommand">The type of the command object associated with the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response object.</typeparam>
    /// <param name="endpoints">The endpoint route builder used to define route mappings.</param>
    /// <param name="endpoint">An instance of an endpoint marker defining metadata for the endpoint.</param>
    /// <param name="handler">The delegate that processes the request.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> representing the configured endpoint and its metadata. Adds default produces behavior and response documentation configuration, such as default response types and status codes.</returns>
    public static RouteHandlerBuilder MapPatch<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        IEndpointMarker endpoint,
        Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();

        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri,handler);
        var result = endpoints.MapPatch(endpointName, handler)
            .AddDefaultProduces<TResponse, TCommand>(classType,handler)
            .Produces(StatusCodes.Status412PreconditionFailed)
            .Produces<TResponse>();

        return result;
    }


    /// <summary>
    /// Maps a PUT request to the specified endpoint using the provided handler and configuration.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command or input model for the endpoint.</typeparam>
    /// <typeparam name="TResponse">The type of the response model from the endpoint.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> used to define the endpoint routes.</param>
    /// <param name="endpoint">The marker interface used to identify endpoint metadata.</param>
    /// <param name="handler">The delegate that will handle the endpoint's processing logic.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further configure the endpoint.</returns>
    public static RouteHandlerBuilder MapPut<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        IEndpointMarker endpoint,
        Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();

        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri,handler);
        var result = endpoints.MapPut(endpointName, handler)
            .AddDefaultProduces<TResponse, TCommand>(classType,handler)
            .Produces(StatusCodes.Status412PreconditionFailed)
            .Produces<TResponse>();
        return result;
    }


    /// Adds default response types, metadata, and tags to the provided RouteHandlerBuilder for a specified endpoint handler.
    /// <param name="builder">The RouteHandlerBuilder to which the default settings will be applied.</param>
    /// <param name="classType">The type of the class that represents the endpoint, used to extract metadata.</param>
    /// <param name="handler">The delegate representing the actual endpoint handler.</param>
    /// <typeparam name="TResponse">The type of the response returned by the endpoint handler.</typeparam>
    /// <typeparam name="TCommand">The type of the command object expected by the endpoint handler.</typeparam>
    /// <returns>The modified RouteHandlerBuilder with the default settings applied.</returns>
    /// Maps an OPTIONS endpoint that returns schema metadata for a resource, using the route
    /// from the <see cref="RouteAttribute"/> on the endpoint marker type.
    /// <typeparam name="TCommand">Marker type used for OpenAPI documentation only — no dispatch occurs.</typeparam>
    /// <typeparam name="TResponse">The schema response DTO type.</typeparam>
    public static RouteHandlerBuilder MapOption<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        IEndpointMarker endpoint,
        Delegate handler)
    {
        Debug.Assert(endpoint != null, nameof(endpoint) + " != null");
        var classType = endpoint.GetType();
        var endpointName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Uri, handler);
        var result = endpoints.MapMethods(endpointName, [HttpMethods.Options], handler)
            .AddDefaultProduces<TResponse, TCommand>(classType, handler)
            .Produces<TResponse>(StatusCodes.Status200OK);
        return result;
    }


    public static RouteHandlerBuilder AddDefaultProduces<TResponse, TCommand>(this RouteHandlerBuilder builder, Type classType, Delegate handler)
    {
        var withName = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Name,handler);
        var withTag = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Tag,handler);
        var withMetadataSummary = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Summary,handler);
        var withMetadataDescription = EndpointV1Adjunct.Get(classType, EndpointAdjunctType.Description,handler);
        builder.Produces<IPermissionFailed<TCommand>>(StatusCodes.Status401Unauthorized)
            .Produces<IValidationfailures>(StatusCodes.Status406NotAcceptable)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(498) //498InvalidToken
            .Produces(499) //499TokenRequired
            .WithName(withName)
            .WithTags(withTag)
            .WithMetadata(new SwaggerOperationAttribute(withMetadataSummary, withMetadataDescription));
        return builder;
    }
}
