using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.Extensions.HttpContextAccessorExtensions;

public static class HttpContextAccessorGetUserAgentExtensions
{
    public static string GetUserAgent(this IHttpContextAccessor httpContextAccessor)
    {
        Debug.Assert(httpContextAccessor != null, nameof(httpContextAccessor) + " != null");
        if (httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            return userAgent;

        }

        return "No-User-Agent";
    }
}