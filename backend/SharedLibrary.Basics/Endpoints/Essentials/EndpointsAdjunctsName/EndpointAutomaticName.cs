// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsName;

/// <summary>
/// Provides an implementation of the IEndpointName interface, allowing
/// automatic retrieval of class names based on type information.
/// </summary>
public class EndpointAutomaticName : IEndpointName
{
    /// <summary>
    /// Attempts to retrieve the name of the provided class type.
    /// </summary>
    /// <param name="classType">The type of the class to retrieve the name from.</param>
    /// <param name="name">When the method returns, contains the name of the class type if the operation succeeded or null if it failed.</param>
    /// <param name="handler">A delegate that may be used in the operation.</param>
    /// <returns>True if the name is successfully retrieved; otherwise, false.</returns>
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        Debug.Assert(classType != null, nameof(classType) + " != null");
        name = classType.Name;
        return true;
    }
}

