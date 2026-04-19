using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Endpoint.Mappers;

public sealed class AdminInviteCodesV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>, AdminInviteCodesV1ListEndpointResponse>
{
    public AdminInviteCodesV1ListEndpointResponse MapFrom(CommandSearchResult<AdminInviteCodesV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminInviteCodesV1ListEndpointResponse
        {
            Items = input.Items.Select(x => new AdminInviteCodesV1ListEndpointResponseItem
            {
                InviteCodeId = x.InviteCodeId,
                Code = x.Code,
                AssignedRole = x.AssignedRole,
                ExpiresAt = x.ExpiresAt,
                RedeemedByUserId = x.RedeemedByUserId,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Total = input.Total,
            Links = []
        };
    }
}
