// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides adjunct methods and utilities for managing and initializing endpoint operations.
/// </summary>
public static class EndpointV1Adjunct
{
    /// <summary>
    /// Holds a reference to the dependency injection service provider to manage service resolutions.
    /// </summary>
    private static IServiceProvider? _serviceProvider; // Store a reference to the DI container.

    // Initialize the service provider (you may do this in your application startup).
    /// <summary>
    /// Initializes the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to set.</param>
    public static void Initialize(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Retrieves the URI or related metadata of the endpoint based on the specified type and adjunct type.
    /// </summary>
    /// <param name="endpointType">The type of the endpoint for which the adjunct is being retrieved.</param>
    /// <param name="type">The adjunct type defining the type of metadata to retrieve (e.g., URI, Summary, Description).</param>
    /// <param name="handler">The delegate handler for the endpoint.</param>
    /// <returns>A string representing the requested metadata or URI of the endpoint, or an appropriate error message if the type is unknown.</returns
    [SuppressMessage("Design", "CA1055:URI-like return values should not be strings")]
    public static string Get(Type endpointType, EndpointAdjunctType type, Delegate handler)
    {
        Debug.Assert(_serviceProvider != null, nameof(_serviceProvider) + " != null - No EndpointV1Adjunct.Initialize() called.");

        var result = type switch
        {
            EndpointAdjunctType.Uri => GetEndpointAdjunctType<IEndpointUri>(endpointType,handler),
            EndpointAdjunctType.Tag => GetEndpointAdjunctType<IEndpointTag>(endpointType,handler),
            EndpointAdjunctType.Summary => GetEndpointAdjunctType<IEndpointSummaryOverwrite>(endpointType, handler),
            EndpointAdjunctType.Description => GetEndpointAdjunctType<IEndpointDescriptionOverwrite>(endpointType, handler),
            EndpointAdjunctType.Name => GetEndpointAdjunctType<IEndpointName>(endpointType,handler),
            _ => throw new InvalidEnumArgumentException($"Unknown action: {nameof(EndpointAdjunctType)}")
        };

        result += type switch
        {
            EndpointAdjunctType.Summary => GetEndpointAdjunctTypeAppend<IEndpointSummaryAppend>(endpointType, handler),
            EndpointAdjunctType.Description => GetEndpointAdjunctTypeAppend<IEndpointDescriptionAppend>(endpointType, handler),
            EndpointAdjunctType.Uri => String.Empty,
            EndpointAdjunctType.Tag => String.Empty,
            EndpointAdjunctType.Name => String.Empty,
            _ => throw new InvalidEnumArgumentException($"Unknown action: {nameof(EndpointAdjunctType)}")
        };

        return result ?? $"Unknown action: {endpointType.Name}";
    }

    /// <summary>
    /// Retrieves the adjunct type information for a specific endpoint and handler.
    /// </summary>
    /// <param name="endpointType">The type of the endpoint whose adjunct property is being retrieved.</param>
    /// <param name="type">The specific type of adjunct information to retrieve (e.g., Uri, Tag, Summary).</param>
    /// <param name="handler">The delegate associated with processing the requested adjunct type.</param>
    /// <returns>A string representing the adjunct type information, or an error message if retrieval failed.</returns>
    private static string? GetEndpointAdjunctType<T>(Type classType, Delegate handler)
        where T : IEndpointBase
    {
        var endpointAdjuncts = _serviceProvider.GetServices<T>();
        foreach (var endpointAdjunct in endpointAdjuncts)
        {
            if (endpointAdjunct.TryGet(classType,out var name,handler))
                return name;
        }

        return String.Empty;
    }

    /// <summary>
    /// Retrieves the adjunct information of a specified type for a given endpoint class and handler.
    /// </summary>
    /// <typeparam name="T">The type of the adjunct interface being retrieved.</typeparam>
    /// <param name="classType">The type of the endpoint class for which the adjunct is being retrieved.</param>
    /// <param name="handler">The delegate used to process the adjunct retrieval.</param>
    /// <returns>A concatenated string containing the adjunct information, or null if no adjuncts are found.</returns>
    private static string? GetEndpointAdjunctTypeAppend<T>(Type classType, Delegate handler)
        where T : IEndpointAppend
    {
        var endpointAdjuncts = _serviceProvider.GetServices<T>();
        var result = String.Empty;
        foreach (var endpointAdjunct in endpointAdjuncts)
        {
            if (endpointAdjunct.TryGet(classType,out var name,handler))
                result += name;
        }

        return result;
    }
}

