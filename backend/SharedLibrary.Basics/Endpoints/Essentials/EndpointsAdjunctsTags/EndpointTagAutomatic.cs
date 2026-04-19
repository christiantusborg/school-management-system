// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;
using QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsTags;

/// <summary>
/// Represents a class for automatically generating tags for endpoints based on their class type.
/// </summary>
public class EndpointTagAutomatic : IEndpointTag
{
    /// <summary>
    /// Attempts to retrieve a formatted tag based on the given class type and handler delegate.
    /// </summary>
    /// <param name="classType">The type of the class for which the tag is to be retrieved.</param>
    /// <param name="tag">The output parameter that will contain the retrieved tag if found.</param>
    /// <param name="handler">A delegate handler for additional processing or validation.</param>
    /// <returns>A boolean value indicating whether the tag was successfully retrieved.</
    [SuppressMessage("Globalization", "CA1304:Specify CultureInfo")]
    [SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version")]
    public bool TryGet(Type classType, out string? tag, Delegate handler)
    {
        Debug.Assert(classType != null, nameof(classType) + " != null");
        var words = MyRegex().Split(classType.Name);
        var endpointTypeName = words[^3] == "Un" ? words[^3] + words[^2] : words[^2];
        var switchResult = endpointTypeName.ToLower() switch
        {
            "create" => string.Join("/", words.Take(words.Length - 3)).ToLower(),
            "init" => string.Join("/", words.Take(words.Length - 3)).ToLower(),
            "finish" => string.Join("/", words.Take(words.Length - 3)).ToLower(),

            "all" => string.Join("/", words.Take(words.Length - 4)).ToLower(),
            "get" => string.Join("/", words.Take(words.Length - 3)).ToLower(),
            "update" => string.Join("/", words.Take(words.Length - 3)).ToLower(),
            "permanent" => string.Join("/", words.Take(words.Length - 4)).ToLower(),
#pragma warning disable CA1862
            "undelete" when words.Length >= 3 && words[^3].ToLower() == "un" => string
#pragma warning restore CA1862
                .Join("/", words.Take(words.Length - 4)).ToLower(),
            "delete" => string.Join("/", words.Take(words.Length - 3)).ToLower(),
            _ => $"Unknown action: {endpointTypeName}"
        };
        tag = switchResult.Capitalize();
        return true;
    }
#pragma warning disable SYSLIB1045
    /// <summary>
    /// Creates a regular expression instance to split a string based on uppercase letters.
    /// </summary>
    /// <returns>A <see cref="Regex"/> instance configured to split a string at uppercase letter boundaries.</returns>
    private static Regex MyRegex()
    {
        return new Regex("(?<!^)(?=[A-Z])");
    }
#pragma warning restore SYSLIB1045
}