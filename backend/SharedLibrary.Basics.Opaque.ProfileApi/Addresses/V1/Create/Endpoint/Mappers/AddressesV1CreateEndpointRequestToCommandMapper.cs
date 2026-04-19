using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint.Mappers;

public class AddressesV1CreateEndpointRequestToCommandMapper
    : IMapper<AddressesV1CreateEndpointRequest, AddressesV1CreateCommand>
{
    public AddressesV1CreateCommand MapFrom(AddressesV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1CreateCommand
        {
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
