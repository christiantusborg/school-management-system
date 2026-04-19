using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.Extensions.HttpContextAccessorExtensions;

public static class HttpContextAccessorGetIpAddressExtensions
{
    public static string GetIpAddress(this IHttpContextAccessor httpContextAccessor)
    {
        Debug.Assert(httpContextAccessor != null, nameof(httpContextAccessor) + " != null");
        var headers = httpContextAccessor.HttpContext?.Request.Headers;

        if (headers != null)
        {
            // Check for the X-Forwarded-For header first (commonly used by proxies)
            if (headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                var forwardedFor = forwarded.ToString();
                var ipAddress = forwardedFor.Split(',')[0].Trim();

                return ipAddress;
            }

            // If X-Forwarded-For header is not available, check for X-Real-IP header
            if (headers.TryGetValue("X-Real-IP", out var real))
            {
                return real.ToString();
            }
        }

        // If no relevant headers are found, use the remote IP address from the connection
        var remoteIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (remoteIpAddress != null)
        {
            if (remoteIpAddress.IsIPv4MappedToIPv6)
            {
                remoteIpAddress = remoteIpAddress.MapToIPv4();
            }

            return remoteIpAddress.ToString();
        }

        return "IP_address_not_available";
    }

}