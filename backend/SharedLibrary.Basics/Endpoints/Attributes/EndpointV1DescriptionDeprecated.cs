// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;

/// <summary>
///     Represents an endpoint adjunct description with deprecated information.
/// </summary>
public class EndpointV1DescriptionDeprecated : IEndpointDescriptionAppend
{
    /// <summary>
    ///     Gets the endpoint handler for the given class type.
    /// </summary>
    /// <param name="classType">The class type.</param>
    /// <returns>
    ///     The endpoint handler as a string. Returns an empty string if the class type is not found. Returns the base
    ///     endpoint handler with deprecation information if applicable.
    /// </returns>
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        var deprecatedAttribute = classType.GetCustomAttribute<DeprecatedAttribute>();

        var eofDeprecated = string.Empty;
        if (deprecatedAttribute == null)
        {
            name = eofDeprecated;
            return true;
        }

        var deprecatedDate = deprecatedAttribute.DeprecatedDate;
        var endOfLifeDate = deprecatedAttribute.EndOfLifeDate;

        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        eofDeprecated = currentDate > endOfLifeDate
            ? $"(EndOfLife: {deprecatedDate}) "
            : $"(Deprecated: {deprecatedDate}) ";

        name = eofDeprecated;
        return true;
    }
}

