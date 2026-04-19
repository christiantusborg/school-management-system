namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Endpoint;

public sealed class AdminUsersV1ResetPasswordEndpointResponse : HateoasBaseResponse
{
    public required string ResetToken { get; init; }
}
