namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Endpoint;

public sealed class AdminUsersV1ChangeRoleEndpointRequest
{
    public required string NewRole { get; init; }
}
