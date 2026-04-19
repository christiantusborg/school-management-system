using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Endpoint.Mappers;

public class AddressesV1SetPrimaryCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AddressesV1SetPrimaryCommandResult, AddressesV1SetPrimaryEndpointResponse>
{
    public AddressesV1SetPrimaryEndpointResponse MapFrom(AddressesV1SetPrimaryCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1SetPrimaryEndpointResponse
        {
            UserAddressId = input.UserAddressId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserAddressId)
        };
    }
}
