using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint.Mappers;

public class AddressesV1UpdateEndpointRequestToCommandMapper
    : IMapper<AddressesV1UpdateEndpointRequest, AddressesV1UpdateCommand>
{
    public AddressesV1UpdateCommand MapFrom(AddressesV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1UpdateCommand
        {
            UserAddressId = Guid.Empty, // overwritten in endpoint
            UserId = string.Empty, // overwritten in endpoint
            Label = input.Label,
            Street = input.Street,
            City = input.City,
            State = input.State,
            ZipCode = input.ZipCode,
            Country = input.Country,
        };
    }
}
