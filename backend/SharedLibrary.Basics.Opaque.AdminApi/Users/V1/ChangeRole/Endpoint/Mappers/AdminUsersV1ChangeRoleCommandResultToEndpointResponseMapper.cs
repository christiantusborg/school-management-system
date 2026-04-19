using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Endpoint.Mappers;

public sealed class AdminUsersV1ChangeRoleCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminUsersV1ChangeRoleCommandResult, AdminUsersV1ChangeRoleEndpointResponse>
{
    public AdminUsersV1ChangeRoleEndpointResponse MapFrom(AdminUsersV1ChangeRoleCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1ChangeRoleEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
