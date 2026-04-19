using Microsoft.AspNetCore.Mvc;
using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsUri;

public class EndpointAttributeUri : IEndpointUri
{
    public bool TryGet(Type classType,out string? name, Delegate handler)
    {
        var attribute = classType.GetCustomAttribute<RouteAttribute>();

        // If the attribute exists, return the URI; otherwise, return null or throw an exception
        if (attribute != null)
        {
            name =  attribute.Template;
            return true;
        }

        name = null;
        return false;
    }
}