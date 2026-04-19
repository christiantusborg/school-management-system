using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint.Mappers;

public class AddressesV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AddressesV1CreateCommandResult, AddressesV1CreateEndpointResponse>
{
    public AddressesV1CreateEndpointResponse MapFrom(AddressesV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1CreateEndpointResponse
        {
            UserAddressId = input.UserAddressId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserAddressId)
        };
    }
}
