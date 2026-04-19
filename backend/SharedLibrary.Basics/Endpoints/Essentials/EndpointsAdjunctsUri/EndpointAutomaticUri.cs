// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuVian.SharedLibrary.Basics.Dispatchers;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsUri;

public class EndpointAutomaticUri : IEndpointUri

{
    /// <summary>
    ///     Gets the URI based on the endpoint type, endpoint type name, words, and parameter.
    /// </summary>
    /// <returns>The generated URI string.</returns>
    [SuppressMessage("Globalization", "CA1304:Specify CultureInfo")]
    [SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version")]
    [SuppressMessage("Performance", "CA1862:Use the \'StringComparison\' method overloads to perform case-insensitive string comparisons")]
    public  bool TryGet(Type classType, out string name, Delegate handler)
    {
        Debug.Assert(classType != null, nameof(classType) + " != null");
        var words = MyRegex().Split(classType.Name);

        // If a version token (V1, V2, …) sits between the action and "Endpoint",
        // the action name is at words[^3] rather than the usual words[^2].
        var isVersioned = words.Length >= 3 && VersionRegex().IsMatch(words[^2]);
        var endpointTypeName = isVersioned
            ? (words.Length >= 5 && words[^4] == "Un" ? words[^4] + words[^3] : words[^3])
            : (words[^3] == "Un" ? words[^3] + words[^2] : words[^2]);

        var parameter = GetEndpointParameter(endpointTypeName, classType, handler);
        var endpointType = classType as MemberInfo;

        var word1 = words[^1];
        var word2 = words[^2];
        var word3 = words[^3];
        var word4 = words[^4];
        var word5 =  "/" + words[^4].ToLower() + "/" + string.Join("/", words[^3]).ToLower();


        name = endpointTypeName.ToLower() switch
        {
            "create" => "/" + words[^3].ToLower() + "/" + string.Join("/", words.Take(words.Length - 3)).ToLower(),
            "init" =>  "/" + words[^4].ToLower() + "/" + string.Join("/", words[^3]).ToLower(),
            "finish" => "/" + words[^4].ToLower() + "/" + words[^3].ToLower(),
            "all" => "/" + words[^4].ToLower() + "/" + string.Join("/", words.Take(words.Length - 4)).ToLower(),
            "get" => "/" + words[^3].ToLower() + "/" + string.Join("/", words.Take(words.Length - 3)).ToLower() +
                     $"/{{{parameter}}}",
            "update" => "/" + words[^3].ToLower() + "/" + string.Join("/", words.Take(words.Length - 3)).ToLower() +
                        $"/{{{parameter}}}",
            "permanent" => "/" + words[^4].ToLower() + "/" + string.Join("/", words.Take(words.Length - 4)).ToLower() +
                           $"/{{{parameter}}}" + "/permanent",
            "undelete" when words.Length >= 3 && words[^3].ToLower() == "un" => "/" + words[^4].ToLower() + "/" +
                                                                              string.Join("/",
                                                                                      words.Take(words.Length - 4))
                                                                                  .ToLower() +
                                                                              $"/{{{parameter}}}/undelete",
            "delete" => "/" + words[^3].ToLower() + "/" + string.Join("/", words.Take(words.Length - 3)).ToLower() +
                        $"/{{{parameter}}}",
            _ => $"Unknown action: {endpointType.Name}"
        };
        return true;
    }
#pragma warning disable SYSLIB1045
    private static Regex MyRegex() => new Regex("(?<!^)(?=[A-Z])");

    private static Regex VersionRegex() => new Regex(@"^V\d+$");
#pragma warning restore SYSLIB1045

    private static string GetEndpointParameter(string endpointName, Type endpointType, Delegate handler)
    {
        if (endpointName is "create" or "getall")
        {
            return "";
        }

        var methodInfo = endpointType.GetMethod(handler.Method.Name, BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo is null)
        {
            return $"Unknown action: {endpointType.Name}";
        }

        var parameterInfos = methodInfo.GetParameters(); //.Where(x => x.);

        var result = parameterInfos.FirstOrDefault(x => x.ParameterType != typeof(IDispatcher) && x.ParameterType != typeof(IHttpContextAccessor))?.Name;

        return result ?? $"Unknown action: {endpointType.Name}";
    }
}

