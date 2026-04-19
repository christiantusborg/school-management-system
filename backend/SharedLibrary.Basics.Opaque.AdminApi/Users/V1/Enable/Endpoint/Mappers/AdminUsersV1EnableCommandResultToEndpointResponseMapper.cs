using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Endpoint.Mappers;

public sealed class AdminUsersV1EnableCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminUsersV1EnableCommandResult, AdminUsersV1EnableEndpointResponse>
{
    public AdminUsersV1EnableEndpointResponse MapFrom(AdminUsersV1EnableCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1EnableEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
