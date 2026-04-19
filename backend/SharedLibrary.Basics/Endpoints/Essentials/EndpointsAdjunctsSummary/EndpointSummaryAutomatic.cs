// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsSummary;

/// <summary>
/// The EndpointAutomaticSummary class is an implementation of the IEndpointSummaryOverwrite interface.
/// It is responsible for generating human-readable summaries for endpoint actions based on the provided class type,
/// utilizing action-specific logic to derive the appropriate description.
/// </summary>
public class EndpointAutomaticSummary : IEndpointSummaryOverwrite
{
    /// <summary>
    /// Attempts to get a summary based on the provided class type and generates a name for the endpoint.
    /// </summary>
    /// <param name="classType">The type of the class to derive the summary from.</param>
    /// <param name="name">The output string containing the generated endpoint name or summary.</param>
    /// <param name="handler">A delegate that can be used for custom handling.</param>
    /// <returns>
    /// A boolean value indicating whether the summary generation was successful.
    /// </returns>
    /// <remarks>
    /// The summary is constructed based on the naming conventions of the class type:
    /// - If the class name contains "Create", it returns a summary for creating a new object.
    /// - If it contains "All", it returns a summary for retrieving objects based on specific search criteria.
    /// - If it contains "Get", it returns a summary for retrieving a single object by its ID.
    /// - If it contains "Update", it returns a summary for updating an existing object.
    /// - If it contains "Permanent", it returns a summary for permanently deleting an object.
    /// - If it contains "Undelete", it returns a summary for restoring a previously deleted object.
    /// - If it contains "Delete", it provides a summary for removing an object.
    /// - Otherwise, it defaults to an unknown action summary.
    /// </remarks>
    [SuppressMessage("Globalization", "CA1304:Specify CultureInfo")]
    [SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version")]
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        Debug.Assert(classType != null, nameof(classType) + " != null");
        var words = MyRegex().Split(classType.Name);
        var endpointTypeName = words[^3] == "Un" ? words[^3] + words[^2] : words[^2];

        var summary = endpointTypeName.ToLower() switch
        {
            "create" => $"Creating a new {words[^4]}",
            "init" => $"Creating a new {words[^4]}",
            "finish" => $"Creating a new {words[^4]}",
            "all" => $"Get {words[^5]} by search criteria",
            "get" => $"Get a {words[^4]} by id",
            "update" => $"Update a {words[^4]} by id",
            "permanent" => $"Permanent delete a {words[^5]} by id",
            "undelete" => $"UnDelete a {words[^5]} by id",
            "delete" => $"Remove a {words[^4]} by id",
            _ => $"Unknown action: {endpointTypeName}"
        };

        name = summary;
        return true;
    }
#pragma warning disable SYSLIB1045
    /// <summary>
    /// Creates a regular expression to split a string based on uppercase letters.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance configured to split a string at positions
    /// where an uppercase letter follows a lowercase letter or another uppercase letter.
    /// </returns>
    private static Regex MyRegex()
    {
        return new Regex("(?<!^)(?=[A-Z])");
    }
#pragma warning restore SYSLIB1045
}

