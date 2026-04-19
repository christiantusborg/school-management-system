namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint;

public sealed class AdminInviteCodesV1CreateEndpointRequest
{
    public required string AssignedRole { get; init; }
    public int ExpirationDays { get; init; } = 7;
}
