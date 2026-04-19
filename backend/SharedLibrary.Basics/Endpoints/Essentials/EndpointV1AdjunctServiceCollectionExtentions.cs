// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsDescription;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsName;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsSummary;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsTags;
using QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsUri;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides extension methods for configuring and registering EndpointV1 Adjunct services with the IServiceCollection.
/// </summary>
[EndpointTag("ClaimsToGroups")]
public static class EndpointV1AdjunctServiceCollectionExtensions
{
    /// <summary>
    /// Adds V1 adjunct services to the provided service collection for endpoint configuration.
    /// </summary>
    /// <param name="services">The service collection to which the services will be added.</param>
    /// <param name="configuration">The configuration used for setting up the adjuncts.</param>
    /// <returns>The updated service collection with adjunct services registered.</returns>
    public static IServiceCollection AddEndpointV1Adjunct(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IEndpointUri, EndpointFileUri>();
        services.AddSingleton<IEndpointUri, EndpointAttributeUri>();
        services.AddSingleton<IEndpointUri, EndpointAutomaticUri>();

        services.AddSingleton<IEndpointTag, EndpointAttributeTag>();
        services.AddSingleton<IEndpointTag, EndpointTagAutomatic>();

        services.AddSingleton<IEndpointSummaryOverwrite, EndpointAttributeSummery>();
        services.AddSingleton<IEndpointSummaryOverwrite, EndpointAutomaticSummary>();

        services.AddSingleton<IEndpointDescriptionOverwrite, EndpointAutomaticDescription>();
        services.AddSingleton<IEndpointName, EndpointAutomaticName>();

        return services;
    }
}

