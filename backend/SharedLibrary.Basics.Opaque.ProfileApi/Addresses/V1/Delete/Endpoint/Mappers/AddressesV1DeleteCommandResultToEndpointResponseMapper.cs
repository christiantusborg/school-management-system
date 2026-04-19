using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Endpoint.Mappers;

public class AddressesV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AddressesV1DeleteCommandResult, AddressesV1DeleteEndpointResponse>
{
    public AddressesV1DeleteEndpointResponse MapFrom(AddressesV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1DeleteEndpointResponse
        {
            UserAddressId = input.UserAddressId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserAddressId)
        };
    }
}
