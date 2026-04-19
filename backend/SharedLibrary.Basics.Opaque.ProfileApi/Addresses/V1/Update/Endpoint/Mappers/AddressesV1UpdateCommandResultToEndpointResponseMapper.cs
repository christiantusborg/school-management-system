using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint.Mappers;

public class AddressesV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AddressesV1UpdateCommandResult, AddressesV1UpdateEndpointResponse>
{
    public AddressesV1UpdateEndpointResponse MapFrom(AddressesV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1UpdateEndpointResponse
        {
            UserAddressId = input.UserAddressId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserAddressId)
        };
    }
}
