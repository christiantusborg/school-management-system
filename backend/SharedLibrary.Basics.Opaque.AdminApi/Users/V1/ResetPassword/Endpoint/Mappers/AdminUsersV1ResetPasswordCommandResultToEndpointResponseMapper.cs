using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Endpoint.Mappers;

public sealed class AdminUsersV1ResetPasswordCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AdminUsersV1ResetPasswordCommandResult, AdminUsersV1ResetPasswordEndpointResponse>
{
    public AdminUsersV1ResetPasswordEndpointResponse MapFrom(AdminUsersV1ResetPasswordCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1ResetPasswordEndpointResponse
        {
            ResetToken = input.ResetToken,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
