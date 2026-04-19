using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Endpoint.Mappers;

public sealed class AdminUsersV1DisableCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminUsersV1DisableCommandResult, AdminUsersV1DisableEndpointResponse>
{
    public AdminUsersV1DisableEndpointResponse MapFrom(AdminUsersV1DisableCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1DisableEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
