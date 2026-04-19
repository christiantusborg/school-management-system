using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Endpoint.Mappers;

public class AddressesV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AddressesV1GetCommandResult, AddressesV1GetEndpointResponse>
{
    public AddressesV1GetEndpointResponse MapFrom(AddressesV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AddressesV1GetEndpointResponse
        {
            UserAddressId = input.UserAddressId,
            Label = input.Label,
            Street = input.Street,
            City = input.City,
            State = input.State,
            ZipCode = input.ZipCode,
            Country = input.Country,
            IsPrimary = input.IsPrimary,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserAddressId)
        };
    }
}
