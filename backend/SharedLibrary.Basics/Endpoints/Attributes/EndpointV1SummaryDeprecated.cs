// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Endpoints.Attributes.Deprecations;

/// <summary>
///     Represents an endpoint adjunct description with deprecated information.
/// </summary>
public class EndpointV1SummaryDeprecated : IEndpointSummaryAppend
{

    public bool TryGet(Type classType, out string? name, Delegate handler)
    {

        var deprecatedAttribute = classType.GetCustomAttribute<DeprecatedAttribute>();

        var eofDeprecated = string.Empty;
        if (deprecatedAttribute == null)
        {
            name = String.Empty;
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

