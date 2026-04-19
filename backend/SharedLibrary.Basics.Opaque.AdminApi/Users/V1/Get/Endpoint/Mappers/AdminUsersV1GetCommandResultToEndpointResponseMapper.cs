using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Endpoint.Mappers;

public sealed class AdminUsersV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminUsersV1GetCommandResult, AdminUsersV1GetEndpointResponse>
{
    public AdminUsersV1GetEndpointResponse MapFrom(AdminUsersV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1GetEndpointResponse
        {
            UserId = input.UserId,
            Username = input.Username,
            Email = input.Email,
            IsEnabled = input.IsEnabled,
            Roles = input.Roles,
            CreatedAt = input.CreatedAt,
            FirstName = input.FirstName,
            LastName = input.LastName,
            AvatarUrl = input.AvatarUrl,
            Bio = input.Bio,
            DateOfBirth = input.DateOfBirth,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, Guid.Parse(input.UserId))
        };
    }
}
