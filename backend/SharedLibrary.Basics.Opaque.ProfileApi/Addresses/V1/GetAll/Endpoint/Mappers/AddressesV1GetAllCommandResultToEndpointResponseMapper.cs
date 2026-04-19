using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Endpoint.Mappers;

public class AddressesV1GetAllCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<AddressesV1GetAllCommandResultItem>, BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem>>
{
    public BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem> MapFrom(CommandSearchResult<AddressesV1GetAllCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new AddressesV1GetAllEndpointResponseItem
        {
            UserAddressId = x.UserAddressId,
            Label = x.Label,
            Street = x.Street,
            City = x.City,
            State = x.State,
            ZipCode = x.ZipCode,
            Country = x.Country,
            IsPrimary = x.IsPrimary,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, x.UserAddressId)
        }).ToList();

        return new BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
