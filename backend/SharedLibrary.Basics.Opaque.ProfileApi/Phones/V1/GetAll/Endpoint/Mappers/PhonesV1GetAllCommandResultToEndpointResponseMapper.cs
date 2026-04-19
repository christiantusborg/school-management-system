using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Endpoint.Mappers;

public class PhonesV1GetAllCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<PhonesV1GetAllCommandResultItem>, BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem>>
{
    public BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem> MapFrom(CommandSearchResult<PhonesV1GetAllCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new PhonesV1GetAllEndpointResponseItem
        {
            UserPhoneId = x.UserPhoneId,
            Number = x.Number,
            Label = x.Label,
            IsPrimary = x.IsPrimary,
            IsVerified = x.IsVerified,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, x.UserPhoneId)
        }).ToList();

        return new BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
