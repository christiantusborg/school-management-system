using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Endpoint.Mappers;

public class EmailsV1GetAllCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<EmailsV1GetAllCommandResultItem>, BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem>>
{
    public BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem> MapFrom(CommandSearchResult<EmailsV1GetAllCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new EmailsV1GetAllEndpointResponseItem
        {
            UserContactEmailId = x.UserContactEmailId,
            Email = x.Email,
            Label = x.Label,
            IsPrimary = x.IsPrimary,
            IsVerified = x.IsVerified,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, x.UserContactEmailId)
        }).ToList();

        return new BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
