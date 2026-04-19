// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsDescription;

/// <summary>
/// Represents a mechanism to automatically generate descriptions for different API endpoints
/// by analyzing the type information and applying predefined logic.
/// </summary>
public class EndpointAutomaticDescription : IEndpointDescriptionOverwrite
{
    /// <summary>
    /// Attempts to retrieve a description based on the given class type and action.
    /// </summary>
    /// <param name="classType">The class type to derive the description from.</param>
    /// <param name="name">The output parameter that stores the description for the given class type.</param>
    /// <param name="handler">The delegate handler used to process the description creation logic.</param>
    /// <returns>
    /// True if the description generation was successful; otherwise, false.
    /// The generated description varies based on the action derived from the class type:
    /// - "Create": Represents creating a new instance of the specified class.
    /// - "All": Represents retrieving the specified class instances by search criteria.
    /// - "Get": Represents retrieving an instance of the specified class by ID.
    /// - "Update": Represents updating an instance of the specified class by ID.
    /// - "Permanent": Represents permanently deleting an instance of the specified class by ID.
    /// - "Undelete": Represents restoring a previously deleted instance of the specified class.
    /// - "Remove": Represents removing an instance of the specified class by ID.
    /// For unknown actions, "Unknown action: [class name]" is returned as the description.
    /// </returns>
    [SuppressMessage("Globalization", "CA1304:Specify CultureInfo")]
    [SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version")]
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        Debug.Assert(classType != null, nameof(classType) + " != null");
        var words = MyRegex().Split(classType.Name);
        var endpointTypeName = words[^3] == "Un" ? words[^3] + words[^2] : words[^2];

        var description = endpointTypeName.ToLower() switch
        {
            "create" => $"Creating a new <i>{words[^4]}</i>",
            "init" => $"Creating a new <i>{words[^4]}</i>",
            "finish" => $"Creating a new <i>{words[^4]}</i>",
            "all" => $"Get <i>{words[^5]}</i> by search criteria",
            "get" => $"Get a <i>{words[^4]}</i> by id",
            "update" => $"Update a <i>{words[^4]}</i> by id",
            "permanent" => $"Permanent delete a <i>{words[^5]}</i> by id",
            "undelete" => $"UnDelete a <i>{words[^5]}</i> by id",
            "delete" => $"Remove a <i>{words[^4]}</i> by id",
            _ => $"Unknown action: {endpointTypeName}"
        };

        name = description;
        return true;
    }
#pragma warning disable SYSLIB1045
    /// <summary>
    /// Splits a given string based on uppercase letters, ensuring that matches are identified where a lowercase
    /// letter is followed by an uppercase letter, or successive uppercase letters are separated.
    /// </summary>
    /// <returns>
    /// A Regex object configured to split strings at boundaries between lowercase and uppercase letters.
    /// </returns>
    private static Regex MyRegex()
    {
        return new Regex("(?<!^)(?=[A-Z])");
    }
#pragma warning restore SYSLIB1045
}

