using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsUri;

public class EndpointFileUri : IEndpointUri
{
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        var className = $"{classType}Uri";

        return EndpointHasFileOverwrite.Get(out name, className, handler);

    }
}