using QuVian.SharedLibrary.Basics.Endpoints.Hateoas;
using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint.Mappers;

public sealed class AdminInviteCodesV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminInviteCodesV1CreateCommandResult, AdminInviteCodesV1CreateEndpointResponse>
{
    public AdminInviteCodesV1CreateEndpointResponse MapFrom(AdminInviteCodesV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminInviteCodesV1CreateEndpointResponse
        {
            Code = input.Code,
            AssignedRole = input.AssignedRole,
            ExpiresAt = input.ExpiresAt,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
