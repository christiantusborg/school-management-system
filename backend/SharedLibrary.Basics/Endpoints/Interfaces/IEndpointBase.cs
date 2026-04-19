namespace QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

public interface  IEndpointBase
{
    bool TryGet(Type classType, out string? name, Delegate handler);
}